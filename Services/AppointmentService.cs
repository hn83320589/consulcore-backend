using consulcore_backend.Data;
using consulcore_backend.DTOs.Appointment;
using consulcore_backend.Exceptions;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace consulcore_backend.Services;

public class AppointmentService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;
    private readonly NotificationService _notify;

    public AppointmentService(AppDbContext db, IAuditService audit, NotificationService notify)
    {
        _db = db;
        _audit = audit;
        _notify = notify;
    }

    public async Task<AppointmentResponseDto> CreateAsync(CreateAppointmentDto dto, int orgId, int createdBy)
    {
        var slot = await _db.ScheduleSlots
            .Include(s => s.TherapistUser)
            .Include(s => s.Room)
            .FirstOrDefaultAsync(s => s.Id == dto.ScheduleSlotId)
            ?? throw new KeyNotFoundException("時段不存在");

        if (slot.Status != SlotStatus.Available)
            throw new BusinessRuleException("此時段已被預約或不可用");

        // 時段衝突：同個案時段重疊（區間重疊，非僅比對起始時間）
        if (await HasClientTimeConflictAsync(dto.ClientProfileId, slot.Date, slot.StartTime, slot.EndTime))
            throw new BusinessRuleException("此個案在重疊時段已有預約");

        // 時段衝突：同心理師時段重疊（理論上 slot 為 reserved 就擋了，但多一層保護）
        if (await HasTherapistTimeConflictAsync(slot.TherapistUserId, slot.Date, slot.StartTime, slot.EndTime))
            throw new BusinessRuleException("此心理師在重疊時段已有預約");

        // BR-06: 溫和提示
        var hints = await GetHintsAsync(dto.ClientProfileId, slot.TherapistUserId);

        slot.Status = SlotStatus.Reserved;
        slot.UpdatedAt = DateTime.UtcNow;

        var appointment = new Appointment
        {
            OrganizationId = orgId,
            ClientProfileId = dto.ClientProfileId,
            TherapistUserId = slot.TherapistUserId,
            RoomId = slot.RoomId,
            ScheduleSlotId = slot.Id,
            SessionType = dto.SessionType,
            VisitType = dto.VisitType,
            Note = dto.Note,
            CreatedBy = createdBy,
        };

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("create", "appointment", appointment.Id,
            $"建立預約 (client={dto.ClientProfileId}, slot={slot.Id})");

        // 通知：appointment_created
        var client = await _db.ClientProfiles.FindAsync(dto.ClientProfileId);
        var vars = new Dictionary<string, string>
        {
            ["client_name"] = client?.Name ?? "",
            ["therapist_name"] = slot.TherapistUser?.DisplayName ?? "",
            ["date"] = slot.Date.ToString("yyyy-MM-dd"),
            ["time"] = slot.StartTime.ToString("HH:mm"),
        };
        if (client?.UserId != null)
            await _notify.SendAsync(NotificationEventType.AppointmentCreated, client.UserId.Value, "client", appointment.OrganizationId, vars);
        await _notify.SendAsync(NotificationEventType.AppointmentCreated, slot.TherapistUserId, "therapist", appointment.OrganizationId, vars);

        var result = await GetByIdAsync(appointment.Id);
        result.Hints = hints;
        return result;
    }

    public async Task<AppointmentResponseDto> CompleteAsync(int id)
    {
        var appointment = await GetEntityAsync(id);
        ValidateStatusTransition(appointment, AppointmentStatus.Completed);

        appointment.Status = AppointmentStatus.Completed;
        appointment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync("complete", "appointment", id, "完成預約");
        return await GetByIdAsync(id);
    }

    public async Task<AppointmentResponseDto> CancelAsync(int id, AppointmentNoteDto? dto)
    {
        var appointment = await _db.Appointments
            .Include(a => a.ScheduleSlot)
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new KeyNotFoundException("預約不存在");

        ValidateStatusTransition(appointment, AppointmentStatus.Cancelled);

        appointment.Status = AppointmentStatus.Cancelled;
        if (dto?.Note != null) appointment.Note = dto.Note;
        appointment.UpdatedAt = DateTime.UtcNow;

        // 釋放 slot
        appointment.ScheduleSlot.Status = SlotStatus.Available;
        appointment.ScheduleSlot.IsReservedHold = false;
        appointment.ScheduleSlot.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.LogAsync("cancel", "appointment", id, "取消預約");

        // 通知：appointment_cancelled
        var client = await _db.ClientProfiles.FindAsync(appointment.ClientProfileId);
        var cancelVars = new Dictionary<string, string>
        {
            ["client_name"] = client?.Name ?? "",
            ["date"] = appointment.ScheduleSlot.Date.ToString("yyyy-MM-dd"),
            ["time"] = appointment.ScheduleSlot.StartTime.ToString("HH:mm"),
        };
        if (client?.UserId != null)
            await _notify.SendAsync(NotificationEventType.AppointmentCancelled, client.UserId.Value, "client", appointment.OrganizationId, cancelVars);
        await _notify.SendAsync(NotificationEventType.AppointmentCancelled, appointment.TherapistUserId, "therapist", appointment.OrganizationId, cancelVars);

        return await GetByIdAsync(id);
    }

    public async Task<AppointmentResponseDto> MarkNoShowAsync(int id)
    {
        var appointment = await GetEntityAsync(id);
        ValidateStatusTransition(appointment, AppointmentStatus.NoShow);

        appointment.Status = AppointmentStatus.NoShow;
        appointment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync("no_show", "appointment", id, "標記未到");
        return await GetByIdAsync(id);
    }

    public async Task<AppointmentResponseDto> RescheduleAsync(int id, RescheduleDto dto)
    {
        var appointment = await _db.Appointments
            .Include(a => a.ScheduleSlot)
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new KeyNotFoundException("預約不存在");

        if (appointment.Status != AppointmentStatus.Scheduled)
            throw new BusinessRuleException("只有已排定的預約可以改期");

        var newSlot = await _db.ScheduleSlots
            .Include(s => s.TherapistUser)
            .Include(s => s.Room)
            .FirstOrDefaultAsync(s => s.Id == dto.NewScheduleSlotId)
            ?? throw new KeyNotFoundException("新時段不存在");

        if (newSlot.Status != SlotStatus.Available)
            throw new BusinessRuleException("新時段已被預約或不可用");

        // 覆寫前先記錄舊 slot id，供稽核使用
        var oldSlotId = appointment.ScheduleSlotId;

        // 釋放原 slot
        appointment.ScheduleSlot.Status = SlotStatus.Available;
        appointment.ScheduleSlot.IsReservedHold = false;
        appointment.ScheduleSlot.UpdatedAt = DateTime.UtcNow;

        // 預約新 slot
        newSlot.Status = SlotStatus.Reserved;
        newSlot.UpdatedAt = DateTime.UtcNow;

        appointment.ScheduleSlotId = newSlot.Id;
        appointment.TherapistUserId = newSlot.TherapistUserId;
        appointment.RoomId = newSlot.RoomId;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _audit.LogAsync("reschedule", "appointment", id,
            $"改期預約 slot {oldSlotId} → {newSlot.Id}");

        return await GetByIdAsync(id);
    }

    public async Task<List<AppointmentResponseDto>> GetByDateRangeAsync(
        DateOnly startDate, DateOnly endDate, int? therapistUserId)
    {
        var query = _db.Appointments
            .Include(a => a.ClientProfile)
            .Include(a => a.TherapistUser)
            .Include(a => a.Room)
            .Include(a => a.ScheduleSlot)
            .Where(a => a.ScheduleSlot.Date >= startDate && a.ScheduleSlot.Date <= endDate);

        if (therapistUserId.HasValue)
            query = query.Where(a => a.TherapistUserId == therapistUserId.Value);

        var appointments = await query
            .OrderBy(a => a.ScheduleSlot.Date)
            .ThenBy(a => a.ScheduleSlot.StartTime)
            .ToListAsync();

        return appointments.Select(MapToDto).ToList();
    }

    public async Task<List<AppointmentResponseDto>> GetListAsync(
        int? clientProfileId, int? therapistUserId, string? status)
    {
        var query = _db.Appointments
            .Include(a => a.ClientProfile)
            .Include(a => a.TherapistUser)
            .Include(a => a.Room)
            .Include(a => a.ScheduleSlot)
            .AsQueryable();

        if (clientProfileId.HasValue)
            query = query.Where(a => a.ClientProfileId == clientProfileId.Value);
        if (therapistUserId.HasValue)
            query = query.Where(a => a.TherapistUserId == therapistUserId.Value);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(a => a.Status == status);

        var appointments = await query
            .OrderByDescending(a => a.ScheduleSlot.Date)
            .ThenByDescending(a => a.ScheduleSlot.StartTime)
            .ToListAsync();

        return appointments.Select(MapToDto).ToList();
    }

    public async Task<List<DTOs.ScheduleSlot.ScheduleSlotResponseDto>> GetAvailableSlotsAsync(
        int? therapistUserId, DateOnly startDate, DateOnly endDate)
    {
        var query = _db.ScheduleSlots
            .Include(s => s.TherapistUser)
            .Include(s => s.Room)
            .Where(s => s.Date >= startDate && s.Date <= endDate
                && s.Status == SlotStatus.Available
                && !s.IsReservedHold);

        if (therapistUserId.HasValue)
            query = query.Where(s => s.TherapistUserId == therapistUserId.Value);

        return await query
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .Select(s => new DTOs.ScheduleSlot.ScheduleSlotResponseDto
            {
                Id = s.Id,
                TherapistUserId = s.TherapistUserId,
                TherapistName = s.TherapistUser.DisplayName,
                RoomId = s.RoomId,
                RoomName = s.Room != null ? s.Room.Name : null,
                Date = s.Date.ToString("yyyy-MM-dd"),
                StartTime = s.StartTime.ToString("HH:mm"),
                EndTime = s.EndTime.ToString("HH:mm"),
                Status = s.Status,
                Note = s.Note,
            })
            .ToListAsync();
    }

    public async Task<AppointmentResponseDto> GetByIdAsync(int id)
    {
        var appointment = await _db.Appointments
            .Include(a => a.ClientProfile)
            .Include(a => a.TherapistUser)
            .Include(a => a.Room)
            .Include(a => a.ScheduleSlot)
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new KeyNotFoundException("預約不存在");

        return MapToDto(appointment);
    }

    private async Task<Appointment> GetEntityAsync(int id)
    {
        return await _db.Appointments.FindAsync(id)
            ?? throw new KeyNotFoundException("預約不存在");
    }

    // 同個案在同日期是否有時段重疊的已排定預約（區間重疊）
    public async Task<bool> HasClientTimeConflictAsync(
        int clientProfileId, DateOnly date, TimeOnly startTime, TimeOnly endTime, int? excludeAppointmentId = null)
    {
        return await _db.Appointments
            .Include(a => a.ScheduleSlot)
            .AnyAsync(a => a.ClientProfileId == clientProfileId
                && a.Status == AppointmentStatus.Scheduled
                && (excludeAppointmentId == null || a.Id != excludeAppointmentId)
                && a.ScheduleSlot.Date == date
                && a.ScheduleSlot.StartTime < endTime
                && a.ScheduleSlot.EndTime > startTime);
    }

    // 同心理師在同日期是否有時段重疊的已排定預約（區間重疊）
    public async Task<bool> HasTherapistTimeConflictAsync(
        int therapistUserId, DateOnly date, TimeOnly startTime, TimeOnly endTime, int? excludeAppointmentId = null)
    {
        return await _db.Appointments
            .Include(a => a.ScheduleSlot)
            .AnyAsync(a => a.TherapistUserId == therapistUserId
                && a.Status == AppointmentStatus.Scheduled
                && (excludeAppointmentId == null || a.Id != excludeAppointmentId)
                && a.ScheduleSlot.Date == date
                && a.ScheduleSlot.StartTime < endTime
                && a.ScheduleSlot.EndTime > startTime);
    }

    private async Task<List<string>> GetHintsAsync(int clientProfileId, int therapistUserId)
    {
        var hints = new List<string>();

        // BR-06: 檢查個案是否有慣用心理師
        var activeTherapists = await _db.TherapistClients
            .Where(tc => tc.ClientProfileId == clientProfileId
                && tc.Status == TherapistClientStatus.Active)
            .Select(tc => tc.TherapistUserId)
            .ToListAsync();

        if (activeTherapists.Count > 0 && !activeTherapists.Contains(therapistUserId))
        {
            hints.Add("此個案目前指派給其他心理師，您可自由選擇，但建議確認是否需要調整指派");
        }

        return hints;
    }

    private static void ValidateStatusTransition(Appointment appointment, string newStatus)
    {
        if (appointment.Status != AppointmentStatus.Scheduled)
            throw new BusinessRuleException(
                $"無法從 {appointment.Status} 轉換至 {newStatus}，只有 scheduled 狀態可操作");
    }

    private static AppointmentResponseDto MapToDto(Appointment a)
    {
        return new AppointmentResponseDto
        {
            Id = a.Id,
            ClientProfileId = a.ClientProfileId,
            ClientName = a.ClientProfile?.Name ?? "",
            TherapistUserId = a.TherapistUserId,
            TherapistName = a.TherapistUser?.DisplayName ?? "",
            RoomId = a.RoomId,
            RoomName = a.Room?.Name,
            ScheduleSlotId = a.ScheduleSlotId,
            Date = a.ScheduleSlot?.Date.ToString("yyyy-MM-dd") ?? "",
            StartTime = a.ScheduleSlot?.StartTime.ToString("HH:mm") ?? "",
            EndTime = a.ScheduleSlot?.EndTime.ToString("HH:mm") ?? "",
            SeriesId = a.SeriesId,
            SessionType = a.SessionType,
            VisitType = a.VisitType,
            Status = a.Status,
            Note = a.Note,
            CreatedAt = a.CreatedAt,
        };
    }
}
