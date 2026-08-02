using consulcore_backend.DTOs.SessionNote;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/session-notes")]
[Authorize]
public class SessionNotesController : ControllerBase
{
    private readonly SessionNoteService _sessionNoteService;

    public SessionNotesController(SessionNoteService sessionNoteService)
    {
        _sessionNoteService = sessionNoteService;
    }

    [HttpPost]
    [Authorize(Policy = "CanCreateSessions")]
    public async Task<IActionResult> Create([FromBody] CreateSessionNoteDto dto)
    {
        var orgId = int.Parse(User.FindFirst("organizationId")!.Value);
        var userId = int.Parse(User.FindFirst("userId")!.Value);
        var note = await _sessionNoteService.CreateAsync(dto, orgId, userId);
        return Ok(ApiResponse<SessionNoteResponseDto>.Ok(note, "晤談紀錄已建立"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanUpdateSessions")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSessionNoteDto dto)
    {
        var userId = int.Parse(User.FindFirst("userId")!.Value);
        var note = await _sessionNoteService.UpdateAsync(id, dto, userId);
        return Ok(ApiResponse<SessionNoteResponseDto>.Ok(note, "晤談紀錄已更新"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanUpdateSessions")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = int.Parse(User.FindFirst("userId")!.Value);
        await _sessionNoteService.DeleteAsync(id, userId);
        return Ok(ApiResponse<object>.Ok(new { }, "晤談紀錄已刪除"));
    }

    [HttpPost("{id}/lock")]
    [Authorize(Policy = "CanLockSessions")]
    public async Task<IActionResult> Lock(int id)
    {
        var userId = int.Parse(User.FindFirst("userId")!.Value);
        var note = await _sessionNoteService.LockAsync(id, userId);
        return Ok(ApiResponse<SessionNoteResponseDto>.Ok(note, "晤談紀錄已鎖定"));
    }

    [HttpPost("{id}/unlock")]
    [Authorize(Policy = "CanLockSessions")]
    public async Task<IActionResult> Unlock(int id)
    {
        var userId = int.Parse(User.FindFirst("userId")!.Value);
        var note = await _sessionNoteService.UnlockAsync(id, userId);
        return Ok(ApiResponse<SessionNoteResponseDto>.Ok(note, "晤談紀錄已解鎖"));
    }

    [HttpGet("by-client/{clientProfileId}")]
    [Authorize(Policy = "CanViewSessions")]
    public async Task<IActionResult> GetByClient(int clientProfileId)
    {
        var notes = await _sessionNoteService.GetByClientAsync(clientProfileId);
        return Ok(ApiResponse<List<SessionNoteResponseDto>>.Ok(notes));
    }

    [HttpGet("by-appointment/{appointmentId}")]
    [Authorize(Policy = "CanViewSessions")]
    public async Task<IActionResult> GetByAppointment(int appointmentId)
    {
        var note = await _sessionNoteService.GetByAppointmentAsync(appointmentId);
        return Ok(ApiResponse<SessionNoteResponseDto?>.Ok(note));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "CanViewSessions")]
    public async Task<IActionResult> GetById(int id)
    {
        var note = await _sessionNoteService.ViewByIdAsync(id);
        return Ok(ApiResponse<SessionNoteResponseDto>.Ok(note));
    }
}
