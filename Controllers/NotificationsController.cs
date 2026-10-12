using consulcore_backend.Authorization;
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
    public async Task<ActionResult<ApiResponse<List<NotificationLogResponseDto>>>> GetMyNotifications()
    {
        var userId = User.GetUserId();
        var notifications = await _service.GetByUserAsync(userId);
        return ApiResponse<List<NotificationLogResponseDto>>.Ok(notifications);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<UnreadCountDto>>> UnreadCount()
    {
        var userId = User.GetUserId();
        var count = await _service.GetUnreadCountAsync(userId);
        return ApiResponse<UnreadCountDto>.Ok(new UnreadCountDto { UnreadCount = count });
    }

    [HttpPost("{id}/mark-read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkRead(int id)
    {
        var userId = User.GetUserId();
        await _service.MarkAsReadAsync(id, userId);
        return ApiResponse<object>.Ok(new { }, "已標記已讀");
    }

    [HttpPost("mark-all-read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllRead()
    {
        var userId = User.GetUserId();
        await _service.MarkAllAsReadAsync(userId);
        return ApiResponse<object>.Ok(new { }, "已全部標記已讀");
    }
}
