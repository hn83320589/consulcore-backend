using consulcore_backend.Data;
using consulcore_backend.DTOs.User;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace consulcore_backend.Services;

public class UserService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public UserService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<List<UserResponseDto>> GetAllAsync()
    {
        return await _db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Select(u => MapToDto(u))
            .ToListAsync();
    }

    public async Task<UserResponseDto> GetByIdAsync(int id)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new KeyNotFoundException("使用者不存在");

        return MapToDto(user);
    }

    public async Task<UserResponseDto> CreateAsync(CreateUserDto dto, int orgId)
    {
        // 檢查 username 是否已存在（跨機構唯一）
        var usernameExists = await _db.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Username == dto.Username);
        if (usernameExists)
            throw new InvalidOperationException("此使用者名稱已被使用");

        // 檢查 email 是否在同機構已存在
        var emailExists = await _db.Users
            .AnyAsync(u => u.Email == dto.Email);
        if (emailExists)
            throw new InvalidOperationException("此 Email 在機構內已被使用");

        var user = new User
        {
            OrganizationId = orgId,
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            DisplayName = dto.DisplayName,
            Phone = dto.Phone
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // 指派角色
        var roles = await _db.Roles
            .Where(r => dto.Roles.Contains(r.Name))
            .ToListAsync();

        if (roles.Count != dto.Roles.Count)
            throw new InvalidOperationException("包含無效的角色名稱");

        foreach (var role in roles)
        {
            _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        }
        await _db.SaveChangesAsync();

        await _audit.LogAsync("create", "user", user.Id, $"建立使用者 {user.Username}");

        // Reload with roles
        return await GetByIdAsync(user.Id);
    }

    public async Task<UserResponseDto> UpdateAsync(int id, UpdateUserDto dto)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new KeyNotFoundException("使用者不存在");

        if (dto.Email != null)
        {
            var emailExists = await _db.Users
                .AnyAsync(u => u.Email == dto.Email && u.Id != id);
            if (emailExists)
                throw new InvalidOperationException("此 Email 在機構內已被使用");
            user.Email = dto.Email;
        }

        if (dto.DisplayName != null) user.DisplayName = dto.DisplayName;
        if (dto.Phone != null) user.Phone = dto.Phone;
        user.UpdatedAt = DateTime.UtcNow;

        // 更新角色
        if (dto.Roles != null)
        {
            var newRoles = await _db.Roles
                .Where(r => dto.Roles.Contains(r.Name))
                .ToListAsync();

            if (newRoles.Count != dto.Roles.Count)
                throw new InvalidOperationException("包含無效的角色名稱");

            // 移除舊角色
            _db.UserRoles.RemoveRange(user.UserRoles);

            // 加入新角色
            foreach (var role in newRoles)
            {
                _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            }
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync("update", "user", user.Id, $"更新使用者 {user.Username}");

        return await GetByIdAsync(user.Id);
    }

    public async Task DeleteAsync(int id)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new KeyNotFoundException("使用者不存在");

        // 不能刪除 owner
        if (user.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == "owner"))
        {
            // Need to load role names
            var roleNames = await _db.UserRoles
                .Where(ur => ur.UserId == id)
                .Include(ur => ur.Role)
                .Select(ur => ur.Role.Name)
                .ToListAsync();

            if (roleNames.Contains("owner"))
                throw new InvalidOperationException("無法刪除機構擁有者帳號");
        }

        _db.UserRoles.RemoveRange(user.UserRoles);
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("delete", "user", id, $"刪除使用者 {user.Username}");
    }

    public async Task ResetPasswordAsync(int id, ResetPasswordDto dto)
    {
        var user = await _db.Users.FindAsync(id)
            ?? throw new KeyNotFoundException("使用者不存在");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync("reset_password", "user", id, $"重設使用者 {user.Username} 密碼");
    }

    public async Task<UserResponseDto> ToggleActiveAsync(int id)
    {
        var user = await _db.Users.FindAsync(id)
            ?? throw new KeyNotFoundException("使用者不存在");

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var action = user.IsActive ? "啟用" : "停用";
        await _audit.LogAsync("toggle_active", "user", id, $"{action}使用者 {user.Username}");

        return await GetByIdAsync(id);
    }

    private static UserResponseDto MapToDto(User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Phone = user.Phone,
            IsActive = user.IsActive,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
