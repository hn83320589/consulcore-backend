using consulcore_backend.Authorization;
using consulcore_backend.DTOs.User;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = "CanManageStaff")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    private bool ActingAsOwner => ActiveRoleScope.ActingAsVerified(Request, User, "owner");

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllAsync();
        return Ok(ApiResponse<List<UserResponseDto>>.Ok(users));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        return Ok(ApiResponse<UserResponseDto>.Ok(user));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        var orgId = int.Parse(User.FindFirst("organizationId")!.Value);
        var user = await _userService.CreateAsync(dto, orgId, ActingAsOwner);
        return Ok(ApiResponse<UserResponseDto>.Ok(user, "使用者已建立"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
    {
        var user = await _userService.UpdateAsync(id, dto, ActingAsOwner);
        return Ok(ApiResponse<UserResponseDto>.Ok(user, "使用者已更新"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _userService.DeleteAsync(id);
        return Ok(ApiResponse<object>.Ok(new { }, "使用者已刪除"));
    }

    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordDto dto)
    {
        await _userService.ResetPasswordAsync(id, dto, ActingAsOwner);
        return Ok(ApiResponse<object>.Ok(new { }, "密碼已重設"));
    }

    [HttpPatch("{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var user = await _userService.ToggleActiveAsync(id, ActingAsOwner);
        return Ok(ApiResponse<UserResponseDto>.Ok(user));
    }
}
