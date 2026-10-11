using consulcore_backend.Authorization;
using consulcore_backend.DTOs.ClientMemo;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/client-memos")]
[Authorize]
public class ClientMemosController : ControllerBase
{
    private readonly ClientMemoService _clientMemoService;

    public ClientMemosController(ClientMemoService clientMemoService)
    {
        _clientMemoService = clientMemoService;
    }

    [HttpGet("by-client/{clientProfileId}")]
    [Authorize(Policy = Policies.CanViewClients)]
    public async Task<IActionResult> GetByClient(int clientProfileId)
    {
        var userId = User.GetUserId();
        var memos = await _clientMemoService.GetByClientAsync(clientProfileId, userId);
        return Ok(ApiResponse<List<ClientMemoResponseDto>>.Ok(memos));
    }

    [HttpPost]
    [Authorize(Policy = Policies.CanUpdateClients)]
    public async Task<IActionResult> Create([FromBody] CreateClientMemoDto dto)
    {
        var orgId = User.GetOrganizationId();
        var userId = User.GetUserId();
        var memo = await _clientMemoService.CreateAsync(dto, orgId, userId);
        return Ok(ApiResponse<ClientMemoResponseDto>.Ok(memo, "個案備忘已建立"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.CanUpdateClients)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateClientMemoDto dto)
    {
        var userId = User.GetUserId();
        var memo = await _clientMemoService.UpdateAsync(id, dto, userId);
        return Ok(ApiResponse<ClientMemoResponseDto>.Ok(memo, "個案備忘已更新"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.CanUpdateClients)]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.GetUserId();
        await _clientMemoService.DeleteAsync(id, userId);
        return Ok(ApiResponse<object>.Ok(new { }, "個案備忘已刪除"));
    }
}
