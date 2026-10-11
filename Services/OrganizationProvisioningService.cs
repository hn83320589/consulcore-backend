using consulcore_backend.Authorization;
using consulcore_backend.Data;
using consulcore_backend.DTOs.Auth;
using consulcore_backend.Exceptions;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace consulcore_backend.Services;

/// <summary>
/// 開設新機構：機構、4 個系統角色與預設權限、owner 帳號、預設初診評估表與通知範本。
/// （自 AuthService.RegisterAsync 拆出；AuthService 只負責登入與發 token）
/// </summary>
public class OrganizationProvisioningService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public OrganizationProvisioningService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<(Organization Organization, User Owner)> ProvisionAsync(RegisterDto dto)
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
            new Role { OrganizationId = org.Id, Name = Roles.Owner, Description = "機構擁有者", IsSystemDefault = true },
            new Role { OrganizationId = org.Id, Name = Roles.Admin, Description = "管理員", IsSystemDefault = true },
            new Role { OrganizationId = org.Id, Name = Roles.Therapist, Description = "心理師", IsSystemDefault = true },
            new Role { OrganizationId = org.Id, Name = Roles.Client, Description = "個案", IsSystemDefault = true },
        };

        _db.Roles.AddRange(defaultRoles);
        await _db.SaveChangesAsync();

        // 3. 映射 Role → Permission
        var allPermissions = await _db.Permissions.ToListAsync();
        var permissionMap = allPermissions.ToDictionary(p => p.Code);

        var rolePermissionMappings = DefaultRolePermissions.ByRole;

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
        var ownerRole = defaultRoles.First(r => r.Name == Roles.Owner);
        _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = ownerRole.Id });
        await _db.SaveChangesAsync();

        // 6. 建立系統預設初診評估表 + 通知範本
        await DatabaseSeeder.SeedDefaultFormTemplateAsync(_db, org.Id, user.Id);
        await DatabaseSeeder.SeedNotificationTemplatesAsync(_db, org.Id);

        await _audit.LogAsync("register", "organization", org.Id, "註冊新機構",
            organizationId: org.Id, userId: user.Id);

        return (org, user);
    }
}
