using consulcore_backend.Authorization;
using consulcore_backend.DTOs.ClientProfile;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/client-profiles")]
[Authorize]
public class ClientProfilesController : ControllerBase
{
    private readonly ClientService _clientService;

    public ClientProfilesController(ClientService clientService)
    {
        _clientService = clientService;
    }

    private int CurrentUserId() => User.GetUserId();

    // owner/admin 可存取全機構個案；其餘角色（therapist）僅限自己被指派的個案。
    // active role 必須是使用者 JWT 內真的擁有的角色，避免 X-Active-Role header 偽造提權。
    private bool HasFullClientScope() =>
        ActiveRoleScope.ActingAsVerified(Request, User, Roles.Owner)
        || ActiveRoleScope.ActingAsVerified(Request, User, Roles.Admin);

    [HttpGet]
    [Authorize(Policy = Policies.CanViewClients)]
    public async Task<IActionResult> GetAll()
    {
        // BR-01：therapist 僅能看到自己被指派的個案；owner/admin 看全機構
        if (!HasFullClientScope())
        {
            var own = await _clientService.GetByTherapistAsync(CurrentUserId());
            return Ok(ApiResponse<List<ClientProfileResponseDto>>.Ok(own));
        }

        var clients = await _clientService.GetAllAsync();
        return Ok(ApiResponse<List<ClientProfileResponseDto>>.Ok(clients));
    }

    [HttpGet("{id}")]
    [Authorize(Policy = Policies.CanViewClients)]
    public async Task<IActionResult> GetById(int id)
    {
        // 代診/交接：therapist 可唯讀非自己指派的個案，但跨心理師檢視須留稽核（見決策 locum-readonly-clinical-records）
        var crossTherapist = !HasFullClientScope()
            && !await _clientService.IsAssignedToTherapistAsync(id, CurrentUserId());

        var client = await _clientService.ViewByIdAsync(id, crossTherapist);
        return Ok(ApiResponse<ClientProfileResponseDto>.Ok(client));
    }

    [HttpGet("by-therapist/{therapistUserId}")]
    [Authorize(Policy = Policies.CanViewClients)]
    public async Task<IActionResult> GetByTherapist(int therapistUserId)
    {
        // therapist 僅能查詢自己的個案清單
        if (!HasFullClientScope() && therapistUserId != CurrentUserId())
            return StatusCode(403, ApiResponse<object>.Fail("無權存取其他心理師的個案"));

        var clients = await _clientService.GetByTherapistAsync(therapistUserId);
        return Ok(ApiResponse<List<ClientProfileResponseDto>>.Ok(clients));
    }

    [HttpGet("my-clients")]
    [Authorize(Policy = Policies.CanViewClients)]
    public async Task<IActionResult> GetMyClients()
    {
        var userId = User.GetUserId();
        var clients = await _clientService.GetByTherapistAsync(userId);
        return Ok(ApiResponse<List<ClientProfileResponseDto>>.Ok(clients));
    }

    [HttpGet("search")]
    [Authorize(Policy = Policies.CanViewClients)]
    public async Task<IActionResult> Search([FromQuery] string? keyword, [FromQuery] string? idNumberLast4)
    {
        // therapist 的搜尋結果僅限自己被指派的個案
        int? scopeTherapistUserId = HasFullClientScope() ? null : CurrentUserId();
        var clients = await _clientService.SearchAsync(keyword, idNumberLast4, scopeTherapistUserId);
        return Ok(ApiResponse<List<ClientProfileResponseDto>>.Ok(clients));
    }

    [HttpPost]
    [Authorize(Policy = Policies.CanCreateClients)]
    public async Task<IActionResult> Create([FromBody] CreateClientProfileDto dto)
    {
        var orgId = User.GetOrganizationId();
        var client = await _clientService.CreateAsync(dto, orgId);
        return Ok(ApiResponse<ClientProfileResponseDto>.Ok(client, "個案已建立"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.CanUpdateClients)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateClientProfileDto dto)
    {
        var client = await _clientService.UpdateAsync(id, dto, CurrentUserId(), HasFullClientScope());
        return Ok(ApiResponse<ClientProfileResponseDto>.Ok(client, "個案已更新"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.CanDeleteClients)]
    public async Task<IActionResult> Delete(int id)
    {
        await _clientService.DeleteAsync(id);
        return Ok(ApiResponse<object>.Ok(new { }, "個案已刪除"));
    }

    [HttpPost("{id}/assign-therapist")]
    [Authorize(Policy = Policies.CanManageClients)]
    public async Task<IActionResult> AssignTherapist(int id, [FromBody] AssignTherapistDto dto)
    {
        var orgId = User.GetOrganizationId();
        var client = await _clientService.AssignTherapistAsync(id, dto, orgId);
        return Ok(ApiResponse<ClientProfileResponseDto>.Ok(client, "心理師已指派"));
    }

    [HttpPost("{id}/transfer-therapist/{currentTherapistUserId}")]
    [Authorize(Policy = Policies.CanManageClients)]
    public async Task<IActionResult> TransferTherapist(
        int id, int currentTherapistUserId, [FromBody] TransferTherapistDto dto)
    {
        var orgId = User.GetOrganizationId();
        var client = await _clientService.TransferTherapistAsync(id, currentTherapistUserId, dto, orgId);
        return Ok(ApiResponse<ClientProfileResponseDto>.Ok(client, "個案已移轉"));
    }

    [HttpPatch("{id}/status")]
    [Authorize(Policy = Policies.CanManageClients)]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeStatusDto dto)
    {
        var client = await _clientService.ChangeStatusAsync(id, dto);
        return Ok(ApiResponse<ClientProfileResponseDto>.Ok(client, "狀態已變更"));
    }
}
