using consulcore_backend.Data;
using consulcore_backend.DTOs.ViolationRecord;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace consulcore_backend.Services;

public class ViolationService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public ViolationService(AppDbContext db, IAuditService audit) { _db = db; _audit = audit; }

    public async Task<List<ViolationRecordResponseDto>> GetAllAsync()
    {
        return await _db.ViolationRecords
            .Include(v => v.TherapistUser)
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => MapToDto(v)).ToListAsync();
    }

    public async Task<ViolationRecordResponseDto> CreateAsync(
        CreateViolationRecordDto dto, int orgId, int reportedBy)
    {
        var v = new Models.ViolationRecord
        {
            OrganizationId = orgId, TherapistUserId = dto.TherapistUserId,
            ReportedBy = reportedBy, IncidentDate = DateOnly.Parse(dto.IncidentDate),
            Severity = dto.Severity, Description = dto.Description,
            ActionTaken = dto.ActionTaken,
        };
        _db.ViolationRecords.Add(v);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("create", "violation_record", v.Id, "建立違規紀錄");
        return await GetByIdAsync(v.Id);
    }

    public async Task<ViolationRecordResponseDto> UpdateAsync(int id, UpdateViolationRecordDto dto)
    {
        var v = await _db.ViolationRecords.FindAsync(id)
            ?? throw new KeyNotFoundException("違規紀錄不存在");
        if (dto.Severity != null) v.Severity = dto.Severity;
        if (dto.Description != null) v.Description = dto.Description;
        if (dto.ActionTaken != null) v.ActionTaken = dto.ActionTaken;
        v.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("update", "violation_record", id, "更新違規紀錄");
        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        var v = await _db.ViolationRecords.FindAsync(id)
            ?? throw new KeyNotFoundException("違規紀錄不存在");
        _db.ViolationRecords.Remove(v);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("delete", "violation_record", id, "刪除違規紀錄");
    }

    public async Task<ViolationRecordResponseDto> ResolveAsync(int id, ResolveViolationDto dto)
    {
        var v = await _db.ViolationRecords.FindAsync(id)
            ?? throw new KeyNotFoundException("違規紀錄不存在");
        v.Status = ViolationStatus.Resolved;
        v.ActionTaken = dto.ActionTaken;
        v.ResolvedAt = DateTime.UtcNow;
        v.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("resolve", "violation_record", id, "解決違規紀錄");
        return await GetByIdAsync(id);
    }

    public async Task<ViolationRecordResponseDto> DismissAsync(int id)
    {
        var v = await _db.ViolationRecords.FindAsync(id)
            ?? throw new KeyNotFoundException("違規紀錄不存在");
        v.Status = ViolationStatus.Dismissed;
        v.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("dismiss", "violation_record", id, "駁回違規紀錄");
        return await GetByIdAsync(id);
    }

    private async Task<ViolationRecordResponseDto> GetByIdAsync(int id)
    {
        var v = await _db.ViolationRecords
            .Include(v => v.TherapistUser)
            .FirstOrDefaultAsync(v => v.Id == id)
            ?? throw new KeyNotFoundException("違規紀錄不存在");
        return MapToDto(v);
    }

    private static ViolationRecordResponseDto MapToDto(Models.ViolationRecord v) => new()
    {
        Id = v.Id, TherapistUserId = v.TherapistUserId,
        TherapistName = v.TherapistUser?.DisplayName ?? "", ReportedBy = v.ReportedBy,
        IncidentDate = v.IncidentDate.ToString("yyyy-MM-dd"), Severity = v.Severity,
        Description = v.Description, ActionTaken = v.ActionTaken,
        Status = v.Status, ResolvedAt = v.ResolvedAt, CreatedAt = v.CreatedAt,
    };
}
