using consulcore_backend.Authorization;
using consulcore_backend.DTOs.FeeSchedule;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/fee-schedules")]
[Authorize(Policy = Policies.CanViewBilling)]
public class FeeSchedulesController : ControllerBase
{
    private readonly FeeScheduleService _service;
    public FeeSchedulesController(FeeScheduleService service) { _service = service; }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<FeeScheduleResponseDto>>>> GetAll()
    {
        var fees = await _service.GetAllAsync();
        return ApiResponse<List<FeeScheduleResponseDto>>.Ok(fees);
    }

    [HttpPost]
    [Authorize(Policy = Policies.CanCreateBilling)]
    public async Task<ActionResult<ApiResponse<FeeScheduleResponseDto>>> Create([FromBody] CreateFeeScheduleDto dto)
    {
        var orgId = User.GetOrganizationId();
        var fee = await _service.CreateAsync(dto, orgId);
        return ApiResponse<FeeScheduleResponseDto>.Ok(fee, "費率已建立");
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.CanUpdateBilling)]
    public async Task<ActionResult<ApiResponse<FeeScheduleResponseDto>>> Update(int id, [FromBody] UpdateFeeScheduleDto dto)
    {
        var fee = await _service.UpdateAsync(id, dto);
        return ApiResponse<FeeScheduleResponseDto>.Ok(fee, "費率已更新");
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.CanUpdateBilling)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return ApiResponse<object>.Ok(new { }, "費率已刪除");
    }
}
