using consulcore_backend.Exceptions;
using consulcore_backend.Authorization;
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
    private readonly ClientService _clientService;

    public ConsentRecordsController(ConsentService consentService, ClientService clientService)
    {
        _consentService = consentService;
        _clientService = clientService;
    }

    // 已用 JWT 驗證的 staff 角色才可跨個案存取；header 偽造只會更受限（fail-closed）
    private bool ActingAsVerifiedNonClient() => ActiveRoleScope.ActingAsVerifiedNonClient(Request, User);
    private Task<int?> CurrentClientProfileIdAsync()
        => _clientService.GetClientProfileIdByUserAsync(User.GetUserId());

    [HttpPost("sign")]
    [Authorize(Policy = Policies.CanCreateConsents)]
    public async Task<ActionResult<ApiResponse<ConsentRecordResponseDto>>> Sign([FromBody] SignConsentDto dto)
    {
        var orgId = User.GetOrganizationId();
        var record = await _consentService.SignConsentAsync(dto, orgId);
        return ApiResponse<ConsentRecordResponseDto>.Ok(record, "同意書已簽署");
    }

    [HttpPost("{id}/void")]
    [Authorize(Policy = Policies.CanVoidConsents)]
    public async Task<ActionResult<ApiResponse<ConsentRecordResponseDto>>> Void(int id, [FromBody] VoidConsentDto dto)
    {
        var record = await _consentService.VoidConsentAsync(id, dto);
        return ApiResponse<ConsentRecordResponseDto>.Ok(record, "同意書已作廢");
    }

    [HttpGet]
    [Authorize(Policy = Policies.CanViewConsents)]
    public async Task<ActionResult<ApiResponse<List<ConsentRecordResponseDto>>>> GetAll(
        [FromQuery] int? clientProfileId, [FromQuery] string? status, [FromQuery] string? triggerEvent)
    {
        // 非 staff 身份一律限縮為自己的簽署紀錄，忽略傳入的 clientProfileId（防越權）
        if (!ActingAsVerifiedNonClient())
        {
            var own = await CurrentClientProfileIdAsync();
            if (own == null) return ApiResponse<List<ConsentRecordResponseDto>>.Ok([]);
            clientProfileId = own;
        }
        var records = await _consentService.GetRecordsAsync(clientProfileId, status, triggerEvent);
        return ApiResponse<List<ConsentRecordResponseDto>>.Ok(records);
    }

    [HttpGet("by-client/{clientProfileId}")]
    [Authorize(Policy = Policies.CanViewConsents)]
    public async Task<ActionResult<ApiResponse<List<ConsentRecordResponseDto>>>> GetByClient(int clientProfileId)
    {
        // 非 staff 身份只能查自己的簽署紀錄（IDOR 防護）
        if (!ActingAsVerifiedNonClient())
        {
            var own = await CurrentClientProfileIdAsync();
            if (own == null || clientProfileId != own)
                throw new ForbiddenException("無權存取其他個案的同意書");
        }

        var records = await _consentService.GetRecordsByClientAsync(clientProfileId);
        return ApiResponse<List<ConsentRecordResponseDto>>.Ok(records);
    }

    [HttpGet("by-trigger-event/{triggerEvent}")]
    [Authorize(Policy = Policies.CanViewConsents)]
    public async Task<ActionResult<ApiResponse<List<ConsentRecordResponseDto>>>> GetByTriggerEvent(string triggerEvent)
    {
        // 依觸發事件跨個案瀏覽為 staff 視圖，非 staff 身份無權存取
        if (!ActingAsVerifiedNonClient())
            throw new ForbiddenException("無權存取此清單");

        var records = await _consentService.GetRecordsByTriggerEventAsync(triggerEvent);
        return ApiResponse<List<ConsentRecordResponseDto>>.Ok(records);
    }
}
