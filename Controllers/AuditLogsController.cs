using consulcore_backend.DTOs.AuditLog;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Policy = "CanViewAudit")]
public class AuditLogsController : ControllerBase
{
    private readonly AuditLogService _service;
    public AuditLogsController(AuditLogService service) { _service = service; }

    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? action, [FromQuery] string? resourceType,
        [FromQuery] int? userId, [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var logs = await _service.GetLogsAsync(action, resourceType, userId, startDate, endDate, page, pageSize);
        return Ok(ApiResponse<List<AuditLogResponseDto>>.Ok(logs));
    }

    [HttpGet("export")]
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

        // 加上 UTF-8 BOM，讓 Excel 正確辨識中文編碼
        var preamble = System.Text.Encoding.UTF8.GetPreamble();
        var body = System.Text.Encoding.UTF8.GetBytes(csv);
        var bytes = new byte[preamble.Length + body.Length];
        Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
        Buffer.BlockCopy(body, 0, bytes, preamble.Length, body.Length);

        return File(bytes, "text/csv; charset=utf-8", "audit-logs.csv");
    }
}
