using consulcore_backend.Authorization;
using consulcore_backend.DTOs.Notification;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/notification-templates")]
[Authorize(Policy = Policies.CanManageNotifications)]
public class NotificationTemplatesController : ControllerBase
{
    private readonly NotificationService _service;
    public NotificationTemplatesController(NotificationService service) { _service = service; }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var templates = await _service.GetAllTemplatesAsync();
        return Ok(ApiResponse<List<NotificationTemplateResponseDto>>.Ok(templates));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNotificationTemplateDto dto)
    {
        var orgId = int.Parse(User.FindFirst("organizationId")!.Value);
        var t = await _service.CreateTemplateAsync(dto, orgId);
        return Ok(ApiResponse<NotificationTemplateResponseDto>.Ok(t, "通知範本已建立"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateNotificationTemplateDto dto)
    {
        var t = await _service.UpdateTemplateAsync(id, dto);
        return Ok(ApiResponse<NotificationTemplateResponseDto>.Ok(t, "通知範本已更新"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteTemplateAsync(id);
        return Ok(ApiResponse<object>.Ok(new { }, "通知範本已刪除"));
    }
}
