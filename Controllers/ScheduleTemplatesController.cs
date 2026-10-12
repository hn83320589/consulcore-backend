using consulcore_backend.Authorization;
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
    [Authorize(Policy = Policies.CanViewSchedules)]
    public async Task<ActionResult<ApiResponse<List<ScheduleTemplateResponseDto>>>> GetAll()
    {
        var templates = await _scheduleService.GetAllTemplatesAsync();
        return ApiResponse<List<ScheduleTemplateResponseDto>>.Ok(templates);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = Policies.CanViewSchedules)]
    public async Task<ActionResult<ApiResponse<ScheduleTemplateResponseDto>>> GetById(int id)
    {
        var template = await _scheduleService.GetTemplateByIdAsync(id);
        return ApiResponse<ScheduleTemplateResponseDto>.Ok(template);
    }

    [HttpPost]
    [Authorize(Policy = Policies.CanCreateSchedules)]
    public async Task<ActionResult<ApiResponse<ScheduleTemplateResponseDto>>> Create([FromBody] CreateScheduleTemplateDto dto)
    {
        var orgId = User.GetOrganizationId();
        var template = await _scheduleService.CreateTemplateAsync(dto, orgId);
        return ApiResponse<ScheduleTemplateResponseDto>.Ok(template, "班表範本已建立");
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.CanUpdateSchedules)]
    public async Task<ActionResult<ApiResponse<ScheduleTemplateResponseDto>>> Update(int id, [FromBody] UpdateScheduleTemplateDto dto)
    {
        var template = await _scheduleService.UpdateTemplateAsync(id, dto);
        return ApiResponse<ScheduleTemplateResponseDto>.Ok(template, "班表範本已更新");
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.CanDeleteSchedules)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await _scheduleService.DeleteTemplateAsync(id);
        return ApiResponse<object>.Ok(new { }, "班表範本已刪除");
    }
}
