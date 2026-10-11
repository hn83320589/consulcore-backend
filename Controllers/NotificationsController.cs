using consulcore_backend.DTOs.Notification;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly NotificationService _service;
    public NotificationsController(NotificationService service) { _service = service; }

    [HttpGet]
    public async Task<IActionResult> GetMyNotifications()
    {
        var userId = int.Parse(User.FindFirst("userId")!.Value);
        var notifications = await _service.GetByUserAsync(userId);
        return Ok(ApiResponse<List<NotificationLogResponseDto>>.Ok(notifications));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount()
    {
        var userId = int.Parse(User.FindFirst("userId")!.Value);
        var count = await _service.GetUnreadCountAsync(userId);
        return Ok(ApiResponse<object>.Ok(new { unreadCount = count }));
    }

    [HttpPost("{id}/mark-read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = int.Parse(User.FindFirst("userId")!.Value);
        await _service.MarkAsReadAsync(id, userId);
        return Ok(ApiResponse<object>.Ok(new { }, "已標記已讀"));
    }

    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = int.Parse(User.FindFirst("userId")!.Value);
        await _service.MarkAllAsReadAsync(userId);
        return Ok(ApiResponse<object>.Ok(new { }, "已全部標記已讀"));
    }
}
