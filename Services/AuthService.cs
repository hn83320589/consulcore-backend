using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using consulcore_backend.Data;
using consulcore_backend.DTOs.Auth;
using consulcore_backend.Exceptions;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace consulcore_backend.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IAuditService _audit;

    public AuthService(AppDbContext db, IConfiguration config, IAuditService audit)
    {
        _db = db;
        _config = config;
        _audit = audit;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
    {
        // Login 需要跨機構查詢，繞過 Global Query Filter
        var user = await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.Organization)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == dto.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            // 帳號存在才知道屬於哪個機構，才能寫進該機構的稽核；不存在的帳號只記應用程式 log
            if (user != null)
            {
                await _audit.LogAsync("login_failed", "user", user.Id, "登入失敗：密碼錯誤",
                    organizationId: user.OrganizationId, userId: user.Id);
            }
            throw new UnauthorizedAccessException("帳號或密碼錯誤");
        }

        if (!user.IsActive)
            throw new UnauthorizedAccessException("帳號已停用");

        if (!user.Organization.IsActive)
            throw new UnauthorizedAccessException("機構已停用");

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        // 檢查是否為個案帳號且需要首次改密碼（需在產生 token 前算出，寫入 claim 供 middleware 強制）
        var mustChangePassword = false;
        if (roles.Contains("client"))
        {
            var clientProfile = await _db.ClientProfiles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.UserId == user.Id && c.OrganizationId == user.OrganizationId);
            if (clientProfile?.MustChangePassword == true)
                mustChangePassword = true;
        }

        var token = GenerateJwtToken(user, roles, mustChangePassword);

        await _audit.LogAsync("login", "user", user.Id, "使用者登入",
            organizationId: user.OrganizationId, userId: user.Id);

        return new LoginResponseDto
        {
            Token = token,
            UserId = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Email = user.Email,
            OrganizationId = user.OrganizationId,
            OrganizationName = user.Organization.Name,
            Roles = roles,
            MustChangePassword = mustChangePassword
        };
    }

    public async Task<LoginResponseDto> RegisterAsync(RegisterDto dto)
    {
        // 檢查 username 是否已存在（跨機構）
        var usernameExists = await _db.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Username == dto.Username);

        if (usernameExists)
            throw new BusinessRuleException("此使用者名稱已被使用");

        // 1. 建立 Organization
        var org = new Organization
        {
            Name = dto.OrganizationName,
            Email = dto.Email,
            Phone = dto.Phone
        };
        _db.Organizations.Add(org);
        await _db.SaveChangesAsync();

        // 2. 建立 4 個預設 Role
        var defaultRoles = new[]
        {
            new Role { OrganizationId = org.Id, Name = "owner", Description = "機構擁有者", IsSystemDefault = true },
            new Role { OrganizationId = org.Id, Name = "admin", Description = "管理員", IsSystemDefault = true },
            new Role { OrganizationId = org.Id, Name = "therapist", Description = "心理師", IsSystemDefault = true },
            new Role { OrganizationId = org.Id, Name = "client", Description = "個案", IsSystemDefault = true },
        };

        _db.Roles.AddRange(defaultRoles);
        await _db.SaveChangesAsync();

        // 3. 映射 Role → Permission
        var allPermissions = await _db.Permissions.ToListAsync();
        var permissionMap = allPermissions.ToDictionary(p => p.Code);

        var rolePermissionMappings = GetDefaultRolePermissions();

        foreach (var role in defaultRoles)
        {
            if (rolePermissionMappings.TryGetValue(role.Name, out var codes))
            {
                foreach (var code in codes)
                {
                    if (permissionMap.TryGetValue(code, out var perm))
                    {
                        _db.RolePermissions.Add(new RolePermission
                        {
                            RoleId = role.Id,
                            PermissionId = perm.Id
                        });
                    }
                }
            }
        }
        await _db.SaveChangesAsync();

        // 4. 建立 Owner User
        var user = new User
        {
            OrganizationId = org.Id,
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            DisplayName = dto.DisplayName,
            Phone = dto.Phone,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // 5. 指派 Owner 角色
        var ownerRole = defaultRoles.First(r => r.Name == "owner");
        _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = ownerRole.Id });
        await _db.SaveChangesAsync();

        // 6. 建立系統預設初診評估表 + 通知範本
        await DatabaseSeeder.SeedDefaultFormTemplateAsync(_db, org.Id, user.Id);
        await DatabaseSeeder.SeedNotificationTemplatesAsync(_db, org.Id);

        // 7. 產生 JWT
        var roles = new List<string> { "owner" };
        var token = GenerateJwtToken(user, roles);

        await _audit.LogAsync("register", "organization", org.Id, "註冊新機構",
            organizationId: org.Id, userId: user.Id);

        return new LoginResponseDto
        {
            Token = token,
            UserId = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Email = user.Email,
            OrganizationId = org.Id,
            OrganizationName = org.Name,
            Roles = roles
        };
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(int userId, int orgId, string? activeRole)
    {
        var user = await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.Organization)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == orgId)
            ?? throw new BusinessRuleException("使用者不存在");

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        // 若此帳號有對應的個案檔，帶出 clientProfileId（個案端查詢自己資料用；userId ≠ clientProfileId）
        var clientProfileId = await _db.ClientProfiles
            .IgnoreQueryFilters()
            .Where(c => c.UserId == userId && c.OrganizationId == orgId)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync();

        // 根據 activeRole 取得對應的權限
        var permissions = new List<string>();
        if (!string.IsNullOrEmpty(activeRole))
        {
            var role = user.UserRoles
                .Select(ur => ur.Role)
                .FirstOrDefault(r => r.Name == activeRole);

            if (role != null)
            {
                permissions = role.RolePermissions
                    .Select(rp => rp.Permission.Code)
                    .ToList();
            }
        }

        return new CurrentUserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Phone = user.Phone,
            OrganizationId = user.OrganizationId,
            OrganizationName = user.Organization.Name,
            Roles = roles,
            ActiveRole = activeRole,
            Permissions = permissions,
            ClientProfileId = clientProfileId
        };
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new BusinessRuleException("使用者不存在");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new BusinessRuleException("目前密碼錯誤"); // 已登入的輸入錯誤 → 400，不是認證失敗

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        // 清除首次改密碼旗標
        var clientProfile = await _db.ClientProfiles
            .FirstOrDefaultAsync(c => c.UserId == userId);
        if (clientProfile?.MustChangePassword == true)
        {
            clientProfile.MustChangePassword = false;
            clientProfile.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        await _audit.LogAsync("change_password", "user", userId, "使用者變更密碼");
    }

    private string GenerateJwtToken(User user, List<string> roles, bool mustChangePassword = false)
    {
        var secretKey = _config["Jwt:SecretKey"]!;
        var issuer = _config["Jwt:Issuer"] ?? "ConsulCore";
        var expiryHours = int.Parse(_config["Jwt:ExpiryHours"] ?? "24");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("userId", user.Id.ToString()),
            new Claim("organizationId", user.OrganizationId.ToString()),
            new Claim("username", user.Username),
            new Claim("roles", JsonSerializer.Serialize(roles)),
            new Claim("mustChangePassword", mustChangePassword ? "true" : "false")
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: null,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiryHours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static Dictionary<string, List<string>> GetDefaultRolePermissions()
    {
        return new Dictionary<string, List<string>>
        {
            ["owner"] =
            [
                "client.view", "client.create", "client.update", "client.delete", "client.manage",
                "appointment.view", "appointment.create", "appointment.update", "appointment.delete",
                "schedule.view", "schedule.create", "schedule.update", "schedule.delete",
                "session.view", "session.create", "session.update", "session.lock",
                "consent.view", "consent.create", "consent.update", "consent.void",
                "billing.view", "billing.create", "billing.update", "billing.refund",
                "notification.manage",
                "staff.manage",
                "room.manage",
                "audit.view",
                "performance.view", "performance.manage",
                "org.settings"
            ],
            ["admin"] =
            [
                "client.view", "client.create", "client.update", "client.manage",
                "appointment.view", "appointment.create", "appointment.update", "appointment.delete",
                "schedule.view", "schedule.create", "schedule.update", "schedule.delete",
                "session.view",
                "consent.view", "consent.create", "consent.update",
                "billing.view", "billing.create", "billing.update",
                "notification.manage",
                "staff.manage",
                "room.manage",
                "audit.view",
                "performance.view"
            ],
            ["therapist"] =
            [
                "client.view", "client.create", "client.update",
                "appointment.view", "appointment.create", "appointment.update",
                "schedule.view",
                "session.view", "session.create", "session.update", "session.lock",
                "consent.view", "consent.create",
                "billing.view"
            ],
            ["client"] =
            [
                "appointment.view",
                "consent.view"
            ]
        };
    }
}
