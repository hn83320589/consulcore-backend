using consulcore_backend.DTOs.Role;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Policy = "CanManageStaff")]
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
        var role = await _rolePermissionService.UpdateRolePermissionsAsync(id, dto);
        return Ok(ApiResponse<RoleResponseDto>.Ok(role, "權限已更新"));
    }
}
