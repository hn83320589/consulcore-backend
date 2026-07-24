using consulcore_backend.DTOs.FeeSchedule;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/fee-schedules")]
[Authorize(Policy = "CanViewBilling")]
public class FeeSchedulesController : ControllerBase
{
    private readonly FeeScheduleService _service;
    public FeeSchedulesController(FeeScheduleService service) { _service = service; }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var fees = await _service.GetAllAsync();
        return Ok(ApiResponse<List<FeeScheduleResponseDto>>.Ok(fees));
    }

    [HttpPost]
    [Authorize(Policy = "CanCreateBilling")]
    public async Task<IActionResult> Create([FromBody] CreateFeeScheduleDto dto)
    {
        var orgId = int.Parse(User.FindFirst("organizationId")!.Value);
        var fee = await _service.CreateAsync(dto, orgId);
        return Ok(ApiResponse<FeeScheduleResponseDto>.Ok(fee, "費率已建立"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanUpdateBilling")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFeeScheduleDto dto)
    {
        var fee = await _service.UpdateAsync(id, dto);
        return Ok(ApiResponse<FeeScheduleResponseDto>.Ok(fee, "費率已更新"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanUpdateBilling")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return Ok(ApiResponse<object>.Ok(new { }, "費率已刪除"));
    }
}
