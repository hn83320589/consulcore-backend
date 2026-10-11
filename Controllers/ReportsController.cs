using consulcore_backend.Authorization;
using consulcore_backend.DTOs.Billing;
using consulcore_backend.DTOs.Report;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Policy = Policies.CanViewBilling)]
public class ReportsController : ControllerBase
{
    private readonly ReportService _reportService;
    public ReportsController(ReportService reportService) { _reportService = reportService; }

    [HttpGet("monthly-revenue")]
    public async Task<IActionResult> MonthlyRevenue([FromQuery] int year, [FromQuery] int month)
    {
        var report = await _reportService.GetMonthlyRevenueAsync(year, month);
        return Ok(ApiResponse<MonthlyRevenueDto>.Ok(report));
    }

    [HttpGet("by-therapist")]
    public async Task<IActionResult> ByTherapist([FromQuery] int year, [FromQuery] int month)
    {
        var report = await _reportService.GetRevenueByTherapistAsync(year, month);
        return Ok(ApiResponse<List<TherapistRevenueDto>>.Ok(report));
    }

    [HttpGet("pending")]
    public async Task<IActionResult> Pending()
    {
        var records = await _reportService.GetPendingPaymentsAsync();
        return Ok(ApiResponse<List<PaymentRecordResponseDto>>.Ok(records));
    }

    [HttpGet("package-stats")]
    public async Task<IActionResult> PackageStats()
    {
        var stats = await _reportService.GetPackageStatsAsync();
        return Ok(ApiResponse<PackageStatsDto>.Ok(stats));
    }
}
