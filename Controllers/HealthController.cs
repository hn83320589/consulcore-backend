using consulcore_backend.DTOs.Health;
using consulcore_backend.Models;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<HealthStatusDto>> Get()
    {
        return ApiResponse<HealthStatusDto>.Ok(new HealthStatusDto { Status = "healthy", Timestamp = DateTime.UtcNow });
    }
}
