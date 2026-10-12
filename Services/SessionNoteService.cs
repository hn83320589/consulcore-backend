using consulcore_backend.Data;
using consulcore_backend.DTOs.SessionNote;
using consulcore_backend.Exceptions;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace consulcore_backend.Services;

public class SessionNoteService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public SessionNoteService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<SessionNoteResponseDto> CreateAsync(
        CreateSessionNoteDto dto, int orgId, int therapistUserId)
    {
        var note = new SessionNote
        {
            OrganizationId = orgId,
            ClientProfileId = dto.ClientProfileId,
            TherapistUserId = therapistUserId,
            AppointmentId = dto.AppointmentId,
            Type = dto.Type,
            Format = dto.Format,
            SoapS = dto.SoapS,
            SoapO = dto.SoapO,
            SoapA = dto.SoapA,
            SoapP = dto.SoapP,
            FreeTextContent = dto.FreeTextContent,
        };

        _db.SessionNotes.Add(note);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("create", "session_note", note.Id,
            $"建立晤談紀錄（個案 #{dto.ClientProfileId}）");

        return await GetByIdAsync(note.Id);
    }

    public async Task<SessionNoteResponseDto> UpdateAsync(
        int id, UpdateSessionNoteDto dto, int therapistUserId)
    {
        var note = await _db.SessionNotes.FindAsync(id)
            ?? throw new KeyNotFoundException("晤談紀錄不存在");

        if (note.TherapistUserId != therapistUserId)
            throw new ForbiddenException("僅建立者可編輯此晤談紀錄");

        if (note.IsLocked)
            throw new BusinessRuleException("此晤談紀錄已鎖定，請先解鎖");

        if (dto.SoapS != null) note.SoapS = dto.SoapS;
        if (dto.SoapO != null) note.SoapO = dto.SoapO;
        if (dto.SoapA != null) note.SoapA = dto.SoapA;
        if (dto.SoapP != null) note.SoapP = dto.SoapP;
        if (dto.FreeTextContent != null) note.FreeTextContent = dto.FreeTextContent;
        note.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.LogAsync("update", "session_note", id, "更新晤談紀錄");

        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id, int therapistUserId)
    {
        var note = await _db.SessionNotes.FindAsync(id)
            ?? throw new KeyNotFoundException("晤談紀錄不存在");

        if (note.TherapistUserId != therapistUserId)
            throw new ForbiddenException("僅建立者可刪除此晤談紀錄");

        if (note.IsLocked)
            throw new BusinessRuleException("已鎖定的晤談紀錄不可刪除");

        _db.SessionNotes.Remove(note);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("delete", "session_note", id, "刪除晤談紀錄");
    }

    public async Task<SessionNoteResponseDto> LockAsync(int id, int therapistUserId)
    {
        var note = await _db.SessionNotes.FindAsync(id)
            ?? throw new KeyNotFoundException("晤談紀錄不存在");

        if (note.TherapistUserId != therapistUserId)
            throw new ForbiddenException("僅建立者可鎖定此晤談紀錄");

        if (note.IsLocked)
            throw new BusinessRuleException("此晤談紀錄已鎖定");

        note.IsLocked = true;
        note.LockedAt = DateTime.UtcNow;
        note.LockedBy = therapistUserId;
        note.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.LogAsync("lock", "session_note", id, "鎖定晤談紀錄");

        return await GetByIdAsync(id);
    }

    // BR-08: 解鎖前快照存 AuditLog.changes
    public async Task<SessionNoteResponseDto> UnlockAsync(int id, int therapistUserId)
    {
        var note = await _db.SessionNotes.FindAsync(id)
            ?? throw new KeyNotFoundException("晤談紀錄不存在");

        if (note.TherapistUserId != therapistUserId)
            throw new ForbiddenException("僅建立者可解鎖此晤談紀錄");

        if (!note.IsLocked)
            throw new BusinessRuleException("此晤談紀錄未鎖定");

        // 解鎖前快照
        var snapshot = new
        {
            note.SoapS,
            note.SoapO,
            note.SoapA,
            note.SoapP,
            note.FreeTextContent,
            note.LockedAt,
            note.LockedBy
        };

        note.IsLocked = false;
        note.LockedAt = null;
        note.LockedBy = null;
        note.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.LogAsync("unlock", "session_note", id,
            "解鎖晤談紀錄", snapshot);

        return await GetByIdAsync(id);
    }

    public async Task<List<SessionNoteResponseDto>> GetByClientAsync(int clientProfileId)
    {
        var notes = await _db.SessionNotes.AsNoTracking()
            .Include(n => n.ClientProfile)
            .Include(n => n.TherapistUser)
            .Where(n => n.ClientProfileId == clientProfileId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        // 檢視稽核：讀取個案晤談紀錄（含加密 SOAP 內容）
        await _audit.LogAsync("view", "session_note", clientProfileId,
            $"檢視個案 {clientProfileId} 的晤談紀錄（{notes.Count} 筆）");

        return notes.Select(MapToDto).ToList();
    }

    public async Task<SessionNoteResponseDto?> GetByAppointmentAsync(int appointmentId)
    {
        var note = await _db.SessionNotes.AsNoTracking()
            .Include(n => n.ClientProfile)
            .Include(n => n.TherapistUser)
            .FirstOrDefaultAsync(n => n.AppointmentId == appointmentId);

        if (note != null)
            await _audit.LogAsync("view", "session_note", note.Id,
                $"檢視晤談紀錄（預約 #{appointmentId}）");

        return note != null ? MapToDto(note) : null;
    }

    private async Task<SessionNoteResponseDto> GetByIdAsync(int id)
    {
        var note = await _db.SessionNotes
            .Include(n => n.ClientProfile)
            .Include(n => n.TherapistUser)
            .FirstOrDefaultAsync(n => n.Id == id)
            ?? throw new KeyNotFoundException("晤談紀錄不存在");

        return MapToDto(note);
    }

    // controller 檢視單筆晤談紀錄用：記錄檢視稽核後回傳
    public async Task<SessionNoteResponseDto> ViewByIdAsync(int id)
    {
        var dto = await GetByIdAsync(id);
        await _audit.LogAsync("view", "session_note", id, $"檢視晤談紀錄 #{id}");
        return dto;
    }

    private static SessionNoteResponseDto MapToDto(SessionNote n)
    {
        return new SessionNoteResponseDto
        {
            Id = n.Id,
            ClientProfileId = n.ClientProfileId,
            ClientName = n.ClientProfile?.Name ?? "",
            TherapistUserId = n.TherapistUserId,
            TherapistName = n.TherapistUser?.DisplayName ?? "",
            AppointmentId = n.AppointmentId,
            Type = n.Type,
            Format = n.Format,
            SoapS = n.SoapS,
            SoapO = n.SoapO,
            SoapA = n.SoapA,
            SoapP = n.SoapP,
            FreeTextContent = n.FreeTextContent,
            IntakeSummaryImported = n.IntakeSummaryImported,
            IsLocked = n.IsLocked,
            LockedAt = n.LockedAt,
            LockedBy = n.LockedBy,
            CreatedAt = n.CreatedAt,
            UpdatedAt = n.UpdatedAt,
        };
    }
}
