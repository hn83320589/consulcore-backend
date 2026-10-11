using consulcore_backend.Authorization;
using consulcore_backend.DTOs.ScheduleSlot;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/schedule-slots")]
[Authorize]
public class ScheduleSlotsController : ControllerBase
{
    private readonly ScheduleService _scheduleService;

    public ScheduleSlotsController(ScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    [HttpGet]
    [Authorize(Policy = Policies.CanViewSchedules)]
    public async Task<IActionResult> GetSlots(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] int? therapistUserId)
    {
        var slots = await _scheduleService.GetSlotsAsync(startDate, endDate, therapistUserId);
        return Ok(ApiResponse<List<ScheduleSlotResponseDto>>.Ok(slots));
    }

    [HttpPost("expand")]
    [Authorize(Policy = Policies.CanCreateSchedules)]
    public async Task<IActionResult> Expand([FromBody] ExpandScheduleDto dto)
    {
        var orgId = User.GetOrganizationId();
        var result = await _scheduleService.ExpandScheduleAsync(dto.StartDate, dto.EndDate, orgId);
        return Ok(ApiResponse<ExpandResultDto>.Ok(result, $"展班完成：新增 {result.CreatedCount} 個時段"));
    }

    [HttpPost("manual-expand")]
    [Authorize(Policy = Policies.CanCreateSchedules)]
    public async Task<IActionResult> ManualExpand([FromBody] ManualExpandDto dto)
    {
        var orgId = User.GetOrganizationId();
        var result = await _scheduleService.ManualExpandAsync(dto.Year, dto.Month, orgId);
        return Ok(ApiResponse<ExpandResultDto>.Ok(result, $"展班完成：新增 {result.CreatedCount} 個時段"));
    }

    [HttpPost("{id}/block")]
    [Authorize(Policy = Policies.CanUpdateSchedules)]
    public async Task<IActionResult> Block(int id, [FromBody] BlockSlotDto dto)
    {
        var slot = await _scheduleService.BlockSlotAsync(id, dto);
        return Ok(ApiResponse<ScheduleSlotResponseDto>.Ok(slot, "時段已封鎖"));
    }

    [HttpPost("{id}/unblock")]
    [Authorize(Policy = Policies.CanUpdateSchedules)]
    public async Task<IActionResult> Unblock(int id)
    {
        var slot = await _scheduleService.UnblockSlotAsync(id);
        return Ok(ApiResponse<ScheduleSlotResponseDto>.Ok(slot, "時段已解除封鎖"));
    }
}
