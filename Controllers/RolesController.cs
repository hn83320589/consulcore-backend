using consulcore_backend.Authorization;
using consulcore_backend.DTOs.Role;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Policy = Policies.CanManageStaff)]
public class RolesController : ControllerBase
{
    private readonly RolePermissionService _rolePermissionService;

    public RolesController(RolePermissionService rolePermissionService)
    {
        _rolePermissionService = rolePermissionService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RoleResponseDto>>>> GetAll()
    {
        var roles = await _rolePermissionService.GetAllRolesAsync();
        return ApiResponse<List<RoleResponseDto>>.Ok(roles);
    }

    [HttpPut("{id}/permissions")]
    public async Task<ActionResult<ApiResponse<RoleResponseDto>>> UpdatePermissions(int id, [FromBody] UpdateRolePermissionsDto dto)
    {
        var actingAsOwner = ActiveRoleScope.ActingAsVerified(Request, User, Roles.Owner);
        var role = await _rolePermissionService.UpdateRolePermissionsAsync(id, dto, actingAsOwner);
        return ApiResponse<RoleResponseDto>.Ok(role, "權限已更新");
    }
}
