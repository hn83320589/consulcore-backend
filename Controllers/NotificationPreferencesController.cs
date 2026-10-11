using consulcore_backend.Authorization;
using consulcore_backend.DTOs.Notification;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/notification-preferences")]
[Authorize]
public class NotificationPreferencesController : ControllerBase
{
    private readonly NotificationService _service;
    public NotificationPreferencesController(NotificationService service) { _service = service; }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = User.GetUserId();
        var prefs = await _service.GetPreferencesAsync(userId);
        return Ok(ApiResponse<List<PreferenceResponseDto>>.Ok(prefs));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdatePreferenceDto dto)
    {
        var userId = User.GetUserId();
        await _service.UpdatePreferenceAsync(userId, dto);
        return Ok(ApiResponse<object>.Ok(new { }, "偏好已更新"));
    }
}
