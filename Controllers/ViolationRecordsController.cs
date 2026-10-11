using consulcore_backend.Authorization;
using consulcore_backend.DTOs.ViolationRecord;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/violation-records")]
[Authorize(Policy = Policies.CanManagePerformance)]
public class ViolationRecordsController : ControllerBase
{
    private readonly ViolationService _service;
    public ViolationRecordsController(ViolationService service) { _service = service; }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var records = await _service.GetAllAsync();
        return Ok(ApiResponse<List<ViolationRecordResponseDto>>.Ok(records));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateViolationRecordDto dto)
    {
        var orgId = User.GetOrganizationId();
        var userId = User.GetUserId();
        var record = await _service.CreateAsync(dto, orgId, userId);
        return Ok(ApiResponse<ViolationRecordResponseDto>.Ok(record, "違規紀錄已建立"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateViolationRecordDto dto)
    {
        var record = await _service.UpdateAsync(id, dto);
        return Ok(ApiResponse<ViolationRecordResponseDto>.Ok(record, "違規紀錄已更新"));
    }

    [HttpPost("{id}/resolve")]
    public async Task<IActionResult> Resolve(int id, [FromBody] ResolveViolationDto dto)
    {
        var record = await _service.ResolveAsync(id, dto);
        return Ok(ApiResponse<ViolationRecordResponseDto>.Ok(record, "違規已解決"));
    }

    [HttpPost("{id}/dismiss")]
    public async Task<IActionResult> Dismiss(int id)
    {
        var record = await _service.DismissAsync(id);
        return Ok(ApiResponse<ViolationRecordResponseDto>.Ok(record, "違規已駁回"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return Ok(ApiResponse<object>.Ok(new { }, "違規紀錄已刪除"));
    }
}
