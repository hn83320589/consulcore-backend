using consulcore_backend.Authorization;
using consulcore_backend.DTOs.Auth;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        return Ok(ApiResponse<LoginResponseDto>.Ok(result, "登入成功"));
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        return Ok(ApiResponse<LoginResponseDto>.Ok(result, "註冊成功"));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.GetUserId();
        var orgId = User.GetOrganizationId();
        var activeRole = Request.Headers["X-Active-Role"].FirstOrDefault();

        var result = await _authService.GetCurrentUserAsync(userId, orgId, activeRole);
        return Ok(ApiResponse<CurrentUserDto>.Ok(result));
    }

    [Authorize]
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = User.GetUserId();
        await _authService.ChangePasswordAsync(userId, dto);
        return Ok(ApiResponse<object>.Ok(new { }, "密碼已變更"));
    }
}
