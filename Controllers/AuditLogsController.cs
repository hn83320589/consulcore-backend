using System.ComponentModel.DataAnnotations;
using consulcore_backend.Authorization;
using consulcore_backend.DTOs.AuditLog;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Policy = Policies.CanViewAudit)]
public class AuditLogsController : ControllerBase
{
    private readonly AuditLogService _service;
    public AuditLogsController(AuditLogService service) { _service = service; }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditLogResponseDto>>>> GetLogs(
        [FromQuery] string? action, [FromQuery] string? resourceType,
        [FromQuery] int? userId, [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate, [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, Paging.MaxPageSize)] int pageSize = Paging.DefaultPageSize, [FromQuery] int? resourceId = null)
    {
        var logs = await _service.GetLogsAsync(action, resourceType, userId, startDate, endDate, page, pageSize, resourceId);
        return ApiResponse<PagedResult<AuditLogResponseDto>>.Ok(logs);
    }

    [HttpGet("export")]
    [ProducesResponseType<FileResult>(StatusCodes.Status200OK, "text/csv", "application/pdf")]
    public async Task<IActionResult> Export(
        [FromQuery] string? action, [FromQuery] string? resourceType,
        [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate,
        [FromQuery] string format = "csv")
    {
        if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
        {
            var pdf = await _service.ExportPdfAsync(action, resourceType, startDate, endDate);
            return File(pdf, "application/pdf", "audit-logs.pdf");
        }

        var csv = await _service.ExportCsvAsync(action, resourceType, startDate, endDate);

        return File(CsvWriter.ToUtf8WithBom(csv), "text/csv; charset=utf-8", "audit-logs.csv");
    }
}
