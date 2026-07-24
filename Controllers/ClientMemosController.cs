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
    [Authorize(Policy = "CanViewClients")]
    public async Task<IActionResult> GetByClient(int clientProfileId)
    {
        var userId = int.Parse(User.FindFirst("userId")!.Value);
        var memos = await _clientMemoService.GetByClientAsync(clientProfileId, userId);
        return Ok(ApiResponse<List<ClientMemoResponseDto>>.Ok(memos));
    }

    [HttpPost]
    [Authorize(Policy = "CanUpdateClients")]
    public async Task<IActionResult> Create([FromBody] CreateClientMemoDto dto)
    {
        var orgId = int.Parse(User.FindFirst("organizationId")!.Value);
        var userId = int.Parse(User.FindFirst("userId")!.Value);
        var memo = await _clientMemoService.CreateAsync(dto, orgId, userId);
        return Ok(ApiResponse<ClientMemoResponseDto>.Ok(memo, "個案備忘已建立"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanUpdateClients")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateClientMemoDto dto)
    {
        var userId = int.Parse(User.FindFirst("userId")!.Value);
        var memo = await _clientMemoService.UpdateAsync(id, dto, userId);
        return Ok(ApiResponse<ClientMemoResponseDto>.Ok(memo, "個案備忘已更新"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanUpdateClients")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = int.Parse(User.FindFirst("userId")!.Value);
        await _clientMemoService.DeleteAsync(id, userId);
        return Ok(ApiResponse<object>.Ok(new { }, "個案備忘已刪除"));
    }
}
