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
    public async Task<IActionResult> GetAll()
    {
        var roles = await _rolePermissionService.GetAllRolesAsync();
        return Ok(ApiResponse<List<RoleResponseDto>>.Ok(roles));
    }

    [HttpPut("{id}/permissions")]
    public async Task<IActionResult> UpdatePermissions(int id, [FromBody] UpdateRolePermissionsDto dto)
    {
        var actingAsOwner = ActiveRoleScope.ActingAsVerified(Request, User, Roles.Owner);
        var role = await _rolePermissionService.UpdateRolePermissionsAsync(id, dto, actingAsOwner);
        return Ok(ApiResponse<RoleResponseDto>.Ok(role, "權限已更新"));
    }
}
