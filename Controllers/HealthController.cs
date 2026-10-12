using consulcore_backend.Data;
using consulcore_backend.DTOs.Health;
using consulcore_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;
    public HealthController(AppDbContext db) { _db = db; }

    /// <summary>資料庫連不上時回 503，讓監控與負載平衡器知道這台不可用</summary>
    [HttpGet]
    [ProducesResponseType<ApiResponse<HealthStatusDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<HealthStatusDto>>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<HealthStatusDto>>> Get()
    {
        var databaseOk = await _db.Database.CanConnectAsync();
        var status = new HealthStatusDto
        {
            Status = databaseOk ? "healthy" : "unhealthy",
            Database = databaseOk ? "ok" : "unreachable",
            Timestamp = DateTime.UtcNow,
        };

        if (databaseOk) return ApiResponse<HealthStatusDto>.Ok(status);
        return StatusCode(StatusCodes.Status503ServiceUnavailable,
            new ApiResponse<HealthStatusDto> { Success = false, Message = "資料庫無法連線", Data = status });
    }
}
