using consulcore_backend.Authorization;
using consulcore_backend.DTOs.PerformanceReview;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/performance-reviews")]
[Authorize]
public class PerformanceReviewsController : ControllerBase
{
    private readonly PerformanceService _service;
    public PerformanceReviewsController(PerformanceService service) { _service = service; }

    [HttpGet]
    [Authorize(Policy = Policies.CanManagePerformance)]
    public async Task<ActionResult<ApiResponse<List<PerformanceReviewResponseDto>>>> GetAll()
    {
        var reviews = await _service.GetAllReviewsAsync();
        return ApiResponse<List<PerformanceReviewResponseDto>>.Ok(reviews);
    }

    [HttpGet("by-therapist/{therapistUserId}")]
    [Authorize(Policy = Policies.CanViewPerformance)]
    public async Task<ActionResult<ApiResponse<List<PerformanceReviewResponseDto>>>> GetByTherapist(int therapistUserId)
    {
        var reviews = await _service.GetByTherapistAsync(therapistUserId);
        return ApiResponse<List<PerformanceReviewResponseDto>>.Ok(reviews);
    }

    [HttpPost]
    [Authorize(Policy = Policies.CanManagePerformance)]
    public async Task<ActionResult<ApiResponse<PerformanceReviewResponseDto>>> Create([FromBody] CreatePerformanceReviewDto dto)
    {
        var orgId = User.GetOrganizationId();
        var userId = User.GetUserId();
        var review = await _service.CreateAsync(dto, orgId, userId);
        return ApiResponse<PerformanceReviewResponseDto>.Ok(review, "考核已建立");
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.CanManagePerformance)]
    public async Task<ActionResult<ApiResponse<PerformanceReviewResponseDto>>> Update(int id, [FromBody] UpdatePerformanceReviewDto dto)
    {
        var review = await _service.UpdateAsync(id, dto);
        return ApiResponse<PerformanceReviewResponseDto>.Ok(review, "考核已更新");
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.CanManagePerformance)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return ApiResponse<object>.Ok(new { }, "考核已刪除");
    }

    [HttpPut("{id}/visibility")]
    [Authorize(Policy = Policies.CanManagePerformance)]
    public async Task<ActionResult<ApiResponse<PerformanceReviewResponseDto>>> SetVisibility(int id, [FromBody] SetVisibilityDto dto)
    {
        var userId = User.GetUserId();
        var review = await _service.SetVisibilityAsync(id, dto, userId);
        return ApiResponse<PerformanceReviewResponseDto>.Ok(review, "可見性已設定");
    }

    [HttpGet("metrics/{therapistUserId}")]
    [Authorize(Policy = Policies.CanViewPerformance)]
    public async Task<ActionResult<ApiResponse<TherapistMetricsDto>>> GetMetrics(int therapistUserId,
        [FromQuery, BindRequired] DateOnly startDate, [FromQuery, BindRequired] DateOnly endDate)
    {
        var metrics = await _service.GetMetricsAsync(
            therapistUserId, startDate, endDate);
        return ApiResponse<TherapistMetricsDto>.Ok(metrics);
    }

    [HttpGet("metrics/{therapistUserId}/trend")]
    [Authorize(Policy = Policies.CanViewPerformance)]
    public async Task<ActionResult<ApiResponse<MetricsTrendDto>>> GetMetricsTrend(int therapistUserId,
        [FromQuery] string metric, [FromQuery, BindRequired] DateOnly startDate, [FromQuery, BindRequired] DateOnly endDate,
        [FromQuery] string interval = "month")
    {
        var trend = await _service.GetMetricsTrendAsync(
            therapistUserId, metric, startDate, endDate);
        return ApiResponse<MetricsTrendDto>.Ok(trend);
    }

    [HttpGet("metrics-comparison")]
    [Authorize(Policy = Policies.CanManagePerformance)]
    public async Task<ActionResult<ApiResponse<List<TherapistMetricsDto>>>> GetComparison(
        [FromQuery, BindRequired] DateOnly startDate, [FromQuery, BindRequired] DateOnly endDate)
    {
        var metrics = await _service.GetMetricsComparisonAsync(
            startDate, endDate);
        return ApiResponse<List<TherapistMetricsDto>>.Ok(metrics);
    }
}
