using consulcore_backend.Authorization;
using consulcore_backend.DTOs.User;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = Policies.CanManageStaff)]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    private bool ActingAsOwner => ActiveRoleScope.ActingAsVerified(Request, User, Roles.Owner);

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UserResponseDto>>>> GetAll()
    {
        var users = await _userService.GetAllAsync();
        return ApiResponse<List<UserResponseDto>>.Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserResponseDto>>> GetById(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        return ApiResponse<UserResponseDto>.Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserResponseDto>>> Create([FromBody] CreateUserDto dto)
    {
        var orgId = User.GetOrganizationId();
        var user = await _userService.CreateAsync(dto, orgId, ActingAsOwner);
        return ApiResponse<UserResponseDto>.Ok(user, "使用者已建立");
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UserResponseDto>>> Update(int id, [FromBody] UpdateUserDto dto)
    {
        var user = await _userService.UpdateAsync(id, dto, ActingAsOwner);
        return ApiResponse<UserResponseDto>.Ok(user, "使用者已更新");
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await _userService.DeleteAsync(id);
        return ApiResponse<object>.Ok(new { }, "使用者已刪除");
    }

    [HttpPost("{id}/reset-password")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(int id, [FromBody] ResetPasswordDto dto)
    {
        await _userService.ResetPasswordAsync(id, dto, ActingAsOwner);
        return ApiResponse<object>.Ok(new { }, "密碼已重設");
    }

    [HttpPatch("{id}/toggle-active")]
    public async Task<ActionResult<ApiResponse<UserResponseDto>>> ToggleActive(int id)
    {
        var user = await _userService.ToggleActiveAsync(id, ActingAsOwner);
        return ApiResponse<UserResponseDto>.Ok(user);
    }
}
