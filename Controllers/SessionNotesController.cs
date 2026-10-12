using consulcore_backend.Authorization;
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
    [Authorize(Policy = Policies.CanCreateSessions)]
    public async Task<ActionResult<ApiResponse<SessionNoteResponseDto>>> Create([FromBody] CreateSessionNoteDto dto)
    {
        var orgId = User.GetOrganizationId();
        var userId = User.GetUserId();
        var note = await _sessionNoteService.CreateAsync(dto, orgId, userId);
        return ApiResponse<SessionNoteResponseDto>.Ok(note, "晤談紀錄已建立");
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.CanUpdateSessions)]
    public async Task<ActionResult<ApiResponse<SessionNoteResponseDto>>> Update(int id, [FromBody] UpdateSessionNoteDto dto)
    {
        var userId = User.GetUserId();
        var note = await _sessionNoteService.UpdateAsync(id, dto, userId);
        return ApiResponse<SessionNoteResponseDto>.Ok(note, "晤談紀錄已更新");
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.CanUpdateSessions)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        var userId = User.GetUserId();
        await _sessionNoteService.DeleteAsync(id, userId);
        return ApiResponse<object>.Ok(new { }, "晤談紀錄已刪除");
    }

    [HttpPost("{id}/lock")]
    [Authorize(Policy = Policies.CanLockSessions)]
    public async Task<ActionResult<ApiResponse<SessionNoteResponseDto>>> Lock(int id)
    {
        var userId = User.GetUserId();
        var note = await _sessionNoteService.LockAsync(id, userId);
        return ApiResponse<SessionNoteResponseDto>.Ok(note, "晤談紀錄已鎖定");
    }

    [HttpPost("{id}/unlock")]
    [Authorize(Policy = Policies.CanLockSessions)]
    public async Task<ActionResult<ApiResponse<SessionNoteResponseDto>>> Unlock(int id)
    {
        var userId = User.GetUserId();
        var note = await _sessionNoteService.UnlockAsync(id, userId);
        return ApiResponse<SessionNoteResponseDto>.Ok(note, "晤談紀錄已解鎖");
    }

    [HttpGet("by-client/{clientProfileId}")]
    [Authorize(Policy = Policies.CanViewSessions)]
    public async Task<ActionResult<ApiResponse<List<SessionNoteResponseDto>>>> GetByClient(int clientProfileId)
    {
        var notes = await _sessionNoteService.GetByClientAsync(clientProfileId);
        return ApiResponse<List<SessionNoteResponseDto>>.Ok(notes);
    }

    [HttpGet("by-appointment/{appointmentId}")]
    [Authorize(Policy = Policies.CanViewSessions)]
    public async Task<ActionResult<ApiResponse<SessionNoteResponseDto?>>> GetByAppointment(int appointmentId)
    {
        var note = await _sessionNoteService.GetByAppointmentAsync(appointmentId);
        return ApiResponse<SessionNoteResponseDto?>.Ok(note);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = Policies.CanViewSessions)]
    public async Task<ActionResult<ApiResponse<SessionNoteResponseDto>>> GetById(int id)
    {
        var note = await _sessionNoteService.ViewByIdAsync(id);
        return ApiResponse<SessionNoteResponseDto>.Ok(note);
    }
}
