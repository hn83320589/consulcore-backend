using consulcore_backend.DTOs.PerformanceReview;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/performance-reviews")]
[Authorize]
public class PerformanceReviewsController : ControllerBase
{
    private readonly PerformanceService _service;
    public PerformanceReviewsController(PerformanceService service) { _service = service; }

    [HttpGet]
    [Authorize(Policy = "CanManagePerformance")]
    public async Task<IActionResult> GetAll()
    {
        var reviews = await _service.GetAllReviewsAsync();
        return Ok(ApiResponse<List<PerformanceReviewResponseDto>>.Ok(reviews));
    }

    [HttpGet("by-therapist/{therapistUserId}")]
    [Authorize(Policy = "CanViewPerformance")]
    public async Task<IActionResult> GetByTherapist(int therapistUserId)
    {
        var reviews = await _service.GetByTherapistAsync(therapistUserId);
        return Ok(ApiResponse<List<PerformanceReviewResponseDto>>.Ok(reviews));
    }

    [HttpPost]
    [Authorize(Policy = "CanManagePerformance")]
    public async Task<IActionResult> Create([FromBody] CreatePerformanceReviewDto dto)
    {
        var orgId = int.Parse(User.FindFirst("organizationId")!.Value);
        var userId = int.Parse(User.FindFirst("userId")!.Value);
        var review = await _service.CreateAsync(dto, orgId, userId);
        return Ok(ApiResponse<PerformanceReviewResponseDto>.Ok(review, "考核已建立"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanManagePerformance")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePerformanceReviewDto dto)
    {
        var review = await _service.UpdateAsync(id, dto);
        return Ok(ApiResponse<PerformanceReviewResponseDto>.Ok(review, "考核已更新"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanManagePerformance")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return Ok(ApiResponse<object>.Ok(new { }, "考核已刪除"));
    }

    [HttpPut("{id}/visibility")]
    [Authorize(Policy = "CanManagePerformance")]
    public async Task<IActionResult> SetVisibility(int id, [FromBody] SetVisibilityDto dto)
    {
        var userId = int.Parse(User.FindFirst("userId")!.Value);
        var review = await _service.SetVisibilityAsync(id, dto, userId);
        return Ok(ApiResponse<PerformanceReviewResponseDto>.Ok(review, "可見性已設定"));
    }

    [HttpGet("metrics/{therapistUserId}")]
    [Authorize(Policy = "CanViewPerformance")]
    public async Task<IActionResult> GetMetrics(int therapistUserId,
        [FromQuery] string startDate, [FromQuery] string endDate)
    {
        var metrics = await _service.GetMetricsAsync(
            therapistUserId, DateOnly.Parse(startDate), DateOnly.Parse(endDate));
        return Ok(ApiResponse<TherapistMetricsDto>.Ok(metrics));
    }

    [HttpGet("metrics-comparison")]
    [Authorize(Policy = "CanManagePerformance")]
    public async Task<IActionResult> GetComparison(
        [FromQuery] string startDate, [FromQuery] string endDate)
    {
        var metrics = await _service.GetMetricsComparisonAsync(
            DateOnly.Parse(startDate), DateOnly.Parse(endDate));
        return Ok(ApiResponse<List<TherapistMetricsDto>>.Ok(metrics));
    }
}
