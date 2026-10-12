using consulcore_backend.Data;
using consulcore_backend.DTOs.FeeSchedule;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace consulcore_backend.Services;

public class FeeScheduleService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public FeeScheduleService(AppDbContext db, IAuditService audit) { _db = db; _audit = audit; }

    public async Task<List<FeeScheduleResponseDto>> GetAllAsync()
    {
        return await _db.FeeSchedules
            .Include(f => f.TherapistUser)
            .OrderBy(f => f.SessionType).ThenBy(f => f.VisitType)
            .Select(f => MapToDto(f))
            .ToListAsync();
    }

    public async Task<FeeScheduleResponseDto> CreateAsync(CreateFeeScheduleDto dto, int orgId)
    {
        var fee = new Models.FeeSchedule
        {
            OrganizationId = orgId, Name = dto.Name,
            SessionType = dto.SessionType, VisitType = dto.VisitType,
            TherapistUserId = dto.TherapistUserId, Amount = dto.Amount,
        };
        _db.FeeSchedules.Add(fee);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("create", "fee_schedule", fee.Id, $"建立費率「{fee.Name}」{AuditFormat.Money(fee.Amount)}");
        return MapToDto(fee);
    }

    public async Task<FeeScheduleResponseDto> UpdateAsync(int id, UpdateFeeScheduleDto dto)
    {
        var fee = await _db.FeeSchedules.FindAsync(id)
            ?? throw new KeyNotFoundException("費率不存在");
        if (dto.Name != null) fee.Name = dto.Name;
        if (dto.Amount.HasValue) fee.Amount = dto.Amount.Value;
        if (dto.IsActive.HasValue) fee.IsActive = dto.IsActive.Value;
        fee.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("update", "fee_schedule", id, $"更新費率「{fee.Name}」");
        return MapToDto(fee);
    }

    public async Task DeleteAsync(int id)
    {
        var fee = await _db.FeeSchedules.FindAsync(id)
            ?? throw new KeyNotFoundException("費率不存在");
        _db.FeeSchedules.Remove(fee);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("delete", "fee_schedule", id, $"刪除費率「{fee.Name}」");
    }

    // BR-09: 心理師專屬 > 機構通用
    public async Task<decimal?> FindApplicableFeeAsync(
        int therapistUserId, string sessionType, string visitType)
    {
        // 先找心理師專屬
        var therapistFee = await _db.FeeSchedules
            .FirstOrDefaultAsync(f => f.TherapistUserId == therapistUserId
                && f.SessionType == sessionType && f.VisitType == visitType && f.IsActive);
        if (therapistFee != null) return therapistFee.Amount;

        // 再找機構通用
        var orgFee = await _db.FeeSchedules
            .FirstOrDefaultAsync(f => f.TherapistUserId == null
                && f.SessionType == sessionType && f.VisitType == visitType && f.IsActive);
        return orgFee?.Amount;
    }

    private static FeeScheduleResponseDto MapToDto(Models.FeeSchedule f) => new()
    {
        Id = f.Id, Name = f.Name, SessionType = f.SessionType, VisitType = f.VisitType,
        TherapistUserId = f.TherapistUserId, TherapistName = f.TherapistUser?.DisplayName,
        Amount = f.Amount, Currency = f.Currency, IsActive = f.IsActive,
    };
}
