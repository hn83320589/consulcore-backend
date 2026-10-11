using consulcore_backend.Authorization;
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
    private readonly ClientService _clientService;

    public AppointmentSeriesController(AppointmentSeriesService seriesService, ClientService clientService)
    {
        _seriesService = seriesService;
        _clientService = clientService;
    }

    // 已用 JWT 驗證的 staff 角色才可跨個案存取；header 偽造只會更受限（fail-closed）
    private bool ActingAsVerifiedNonClient() => ActiveRoleScope.ActingAsVerifiedNonClient(Request, User);
    private Task<int?> CurrentClientProfileIdAsync()
        => _clientService.GetClientProfileIdByUserAsync(int.Parse(User.FindFirst("userId")!.Value));

    [HttpPost]
    [Authorize(Policy = Policies.CanCreateAppointments)]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentSeriesDto dto)
    {
        var orgId = int.Parse(User.FindFirst("organizationId")!.Value);
        var userId = int.Parse(User.FindFirst("userId")!.Value);
        var series = await _seriesService.CreateAsync(dto, orgId, userId);
        return Ok(ApiResponse<AppointmentSeriesResponseDto>.Ok(series, "常態性預約已建立"));
    }

    [HttpPost("{id}/renew")]
    [Authorize(Policy = Policies.CanCreateAppointments)]
    public async Task<IActionResult> Renew(int id, [FromBody] RenewSeriesDto dto)
    {
        var orgId = int.Parse(User.FindFirst("organizationId")!.Value);
        var userId = int.Parse(User.FindFirst("userId")!.Value);
        var series = await _seriesService.RenewAsync(id, dto, orgId, userId);
        return Ok(ApiResponse<AppointmentSeriesResponseDto>.Ok(series, "常態性預約已續約"));
    }

    [HttpPost("{id}/end")]
    [Authorize(Policy = Policies.CanUpdateAppointments)]
    public async Task<IActionResult> End(int id)
    {
        var series = await _seriesService.EndAsync(id);
        return Ok(ApiResponse<AppointmentSeriesResponseDto>.Ok(series, "常態性預約已結束"));
    }

    [HttpGet("pending-renewal")]
    [Authorize(Policy = Policies.CanViewAppointments)]
    public async Task<IActionResult> PendingRenewal()
    {
        // 待續約清單為 staff 聚合視圖，非 staff 身份無權存取
        if (!ActingAsVerifiedNonClient())
            return StatusCode(403, ApiResponse<object>.Fail("無權存取此清單"));

        var series = await _seriesService.GetPendingRenewalAsync();
        return Ok(ApiResponse<List<AppointmentSeriesResponseDto>>.Ok(series));
    }

    [HttpGet("by-client/{clientProfileId}")]
    [Authorize(Policy = Policies.CanViewAppointments)]
    public async Task<IActionResult> GetByClient(int clientProfileId, [FromQuery] string? status)
    {
        // 非 staff 身份只能查自己的常態預約，防以任意 id 讀取他人（IDOR）
        if (!ActingAsVerifiedNonClient())
        {
            var own = await CurrentClientProfileIdAsync();
            if (own == null || clientProfileId != own)
                return StatusCode(403, ApiResponse<object>.Fail("無權存取其他個案的常態預約"));
        }

        var series = await _seriesService.GetByClientAsync(clientProfileId, status);
        return Ok(ApiResponse<List<AppointmentSeriesResponseDto>>.Ok(series));
    }
}
