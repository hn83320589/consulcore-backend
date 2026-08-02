using consulcore_backend.DTOs.ConsentRecord;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/consent-records")]
[Authorize]
public class ConsentRecordsController : ControllerBase
{
    private readonly ConsentService _consentService;

    public ConsentRecordsController(ConsentService consentService)
    {
        _consentService = consentService;
    }

    [HttpPost("sign")]
    [Authorize(Policy = "CanCreateConsents")]
    public async Task<IActionResult> Sign([FromBody] SignConsentDto dto)
    {
        var orgId = int.Parse(User.FindFirst("organizationId")!.Value);
        var record = await _consentService.SignConsentAsync(dto, orgId);
        return Ok(ApiResponse<ConsentRecordResponseDto>.Ok(record, "同意書已簽署"));
    }

    [HttpPost("{id}/void")]
    [Authorize(Policy = "CanVoidConsents")]
    public async Task<IActionResult> Void(int id, [FromBody] VoidConsentDto dto)
    {
        var record = await _consentService.VoidConsentAsync(id, dto);
        return Ok(ApiResponse<ConsentRecordResponseDto>.Ok(record, "同意書已作廢"));
    }

    [HttpGet]
    [Authorize(Policy = "CanViewConsents")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? clientProfileId, [FromQuery] string? status, [FromQuery] string? triggerEvent)
    {
        var records = await _consentService.GetRecordsAsync(clientProfileId, status, triggerEvent);
        return Ok(ApiResponse<List<ConsentRecordResponseDto>>.Ok(records));
    }

    [HttpGet("by-client/{clientProfileId}")]
    [Authorize(Policy = "CanViewConsents")]
    public async Task<IActionResult> GetByClient(int clientProfileId)
    {
        var records = await _consentService.GetRecordsByClientAsync(clientProfileId);
        return Ok(ApiResponse<List<ConsentRecordResponseDto>>.Ok(records));
    }

    [HttpGet("by-trigger-event/{triggerEvent}")]
    [Authorize(Policy = "CanViewConsents")]
    public async Task<IActionResult> GetByTriggerEvent(string triggerEvent)
    {
        var records = await _consentService.GetRecordsByTriggerEventAsync(triggerEvent);
        return Ok(ApiResponse<List<ConsentRecordResponseDto>>.Ok(records));
    }
}
