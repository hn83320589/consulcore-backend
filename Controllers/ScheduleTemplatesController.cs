using consulcore_backend.DTOs.ScheduleTemplate;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/schedule-templates")]
[Authorize]
public class ScheduleTemplatesController : ControllerBase
{
    private readonly ScheduleService _scheduleService;

    public ScheduleTemplatesController(ScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    [HttpGet]
    [Authorize(Policy = "CanViewSchedules")]
    public async Task<IActionResult> GetAll()
    {
        var templates = await _scheduleService.GetAllTemplatesAsync();
        return Ok(ApiResponse<List<ScheduleTemplateResponseDto>>.Ok(templates));
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "CanViewSchedules")]
    public async Task<IActionResult> GetById(int id)
    {
        var template = await _scheduleService.GetTemplateByIdAsync(id);
        return Ok(ApiResponse<ScheduleTemplateResponseDto>.Ok(template));
    }

    [HttpPost]
    [Authorize(Policy = "CanCreateSchedules")]
    public async Task<IActionResult> Create([FromBody] CreateScheduleTemplateDto dto)
    {
        var orgId = int.Parse(User.FindFirst("organizationId")!.Value);
        var template = await _scheduleService.CreateTemplateAsync(dto, orgId);
        return Ok(ApiResponse<ScheduleTemplateResponseDto>.Ok(template, "班表範本已建立"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanUpdateSchedules")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateScheduleTemplateDto dto)
    {
        var template = await _scheduleService.UpdateTemplateAsync(id, dto);
        return Ok(ApiResponse<ScheduleTemplateResponseDto>.Ok(template, "班表範本已更新"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanDeleteSchedules")]
    public async Task<IActionResult> Delete(int id)
    {
        await _scheduleService.DeleteTemplateAsync(id);
        return Ok(ApiResponse<object>.Ok(new { }, "班表範本已刪除"));
    }
}
