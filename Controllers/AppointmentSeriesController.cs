using consulcore_backend.DTOs.AppointmentSeries;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/appointment-series")]
[Authorize]
public class AppointmentSeriesController : ControllerBase
{
    private readonly AppointmentSeriesService _seriesService;

    public AppointmentSeriesController(AppointmentSeriesService seriesService)
    {
        _seriesService = seriesService;
    }

    [HttpPost]
    [Authorize(Policy = "CanCreateAppointments")]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentSeriesDto dto)
    {
        var orgId = int.Parse(User.FindFirst("organizationId")!.Value);
        var userId = int.Parse(User.FindFirst("userId")!.Value);
        var series = await _seriesService.CreateAsync(dto, orgId, userId);
        return Ok(ApiResponse<AppointmentSeriesResponseDto>.Ok(series, "常態性預約已建立"));
    }

    [HttpPost("{id}/renew")]
    [Authorize(Policy = "CanCreateAppointments")]
    public async Task<IActionResult> Renew(int id, [FromBody] RenewSeriesDto dto)
    {
        var orgId = int.Parse(User.FindFirst("organizationId")!.Value);
        var userId = int.Parse(User.FindFirst("userId")!.Value);
        var series = await _seriesService.RenewAsync(id, dto, orgId, userId);
        return Ok(ApiResponse<AppointmentSeriesResponseDto>.Ok(series, "常態性預約已續約"));
    }

    [HttpPost("{id}/end")]
    [Authorize(Policy = "CanUpdateAppointments")]
    public async Task<IActionResult> End(int id)
    {
        var series = await _seriesService.EndAsync(id);
        return Ok(ApiResponse<AppointmentSeriesResponseDto>.Ok(series, "常態性預約已結束"));
    }

    [HttpGet("pending-renewal")]
    [Authorize(Policy = "CanViewAppointments")]
    public async Task<IActionResult> PendingRenewal()
    {
        var series = await _seriesService.GetPendingRenewalAsync();
        return Ok(ApiResponse<List<AppointmentSeriesResponseDto>>.Ok(series));
    }
}
