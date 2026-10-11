using consulcore_backend.Data;
using consulcore_backend.DTOs.Role;
using consulcore_backend.Exceptions;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace consulcore_backend.Services;

public class RolePermissionService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public RolePermissionService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<List<RoleResponseDto>> GetAllRolesAsync()
    {
        return await _db.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Select(r => new RoleResponseDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsSystemDefault = r.IsSystemDefault,
                Permissions = r.RolePermissions
                    .Select(rp => rp.Permission.Code)
                    .ToList()
            })
            .ToListAsync();
    }

    public async Task<RoleResponseDto> UpdateRolePermissionsAsync(
        int roleId, UpdateRolePermissionsDto dto, bool actorIsOwner)
    {
        var role = await _db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == roleId)
            ?? throw new KeyNotFoundException("角色不存在");

        // 系統角色（含 admin 自己）的權限只能由擁有者調整，避免 admin 替自己加權限
        if (role.IsSystemDefault && !actorIsOwner)
            throw new ForbiddenException("只有機構擁有者可以調整系統角色的權限");

        // 驗證所有 permission codes 都存在
        var permissions = await _db.Permissions
            .Where(p => dto.PermissionCodes.Contains(p.Code))
            .ToListAsync();

        if (permissions.Count != dto.PermissionCodes.Count)
        {
            var validCodes = permissions.Select(p => p.Code).ToHashSet();
            var invalidCodes = dto.PermissionCodes.Where(c => !validCodes.Contains(c)).ToList();
            throw new InvalidOperationException($"無效的權限代碼：{string.Join(", ", invalidCodes)}");
        }

        var beforePermissions = role.RolePermissions
            .Select(rp => rp.PermissionId).ToList();

        // 移除舊的
        _db.RolePermissions.RemoveRange(role.RolePermissions);

        // 加入新的
        foreach (var perm in permissions)
        {
            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = perm.Id
            });
        }

        role.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync("update_permissions", "role", roleId,
            $"更新角色 {role.Name} 的權限",
            new { before = beforePermissions, after = dto.PermissionCodes });

        return await GetRoleByIdAsync(roleId);
    }

    public async Task<List<string>> GetPermissionsForRoleAsync(string roleName, int orgId)
    {
        return await _db.Roles
            .IgnoreQueryFilters()
            .Where(r => r.OrganizationId == orgId && r.Name == roleName)
            .SelectMany(r => r.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .ToListAsync();
    }

    private async Task<RoleResponseDto> GetRoleByIdAsync(int roleId)
    {
        var role = await _db.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == roleId)
            ?? throw new KeyNotFoundException("角色不存在");

        return new RoleResponseDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystemDefault = role.IsSystemDefault,
            Permissions = role.RolePermissions.Select(rp => rp.Permission.Code).ToList()
        };
    }
}
