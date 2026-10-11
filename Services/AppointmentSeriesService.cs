using consulcore_backend.Authorization;
using consulcore_backend.Data;
using consulcore_backend.DTOs.AppointmentSeries;
using consulcore_backend.Exceptions;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace consulcore_backend.Services;

public class AppointmentSeriesService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;
    private readonly NotificationService _notify;
    private readonly AppointmentService _appointments;

    public AppointmentSeriesService(
        AppDbContext db, IAuditService audit, NotificationService notify, AppointmentService appointments)
    {
        _db = db;
        _audit = audit;
        _notify = notify;
        _appointments = appointments;
    }

    // 台灣時區固定 UTC+8（無日光節約），以此計算「今天」避免 UTC 邊界日誤判
    private static DateOnly LocalToday() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(8));

    public async Task<AppointmentSeriesResponseDto> CreateAsync(
        CreateAppointmentSeriesDto dto, int orgId, int createdBy)
    {
        // BR-07: 上限 3 個月
        if (dto.Months > 3)
            throw new BusinessRuleException("常態性預約上限為 3 個月");

        var startTime = TimeOnly.Parse(dto.StartTime);
        var expiresAt = LocalToday().AddMonths(dto.Months);

        var series = new AppointmentSeries
        {
            OrganizationId = orgId,
            ClientProfileId = dto.ClientProfileId,
            TherapistUserId = dto.TherapistUserId,
            DayOfWeek = dto.DayOfWeek,
            StartTime = startTime,
            Frequency = dto.Frequency,
            ExpiresAt = expiresAt,
        };

        _db.AppointmentSeries.Add(series);
        await _db.SaveChangesAsync();

        // 自動產生 Appointment
        var created = await GenerateAppointmentsAsync(series, orgId, createdBy);

        await _audit.LogAsync("create", "appointment_series", series.Id,
            $"建立常態性預約 (client={dto.ClientProfileId}, therapist={dto.TherapistUserId}), 產生 {created} 筆預約");

        return await GetByIdAsync(series.Id);
    }

    public async Task<AppointmentSeriesResponseDto> RenewAsync(int seriesId, RenewSeriesDto dto, int orgId, int createdBy)
    {
        var series = await _db.AppointmentSeries.FindAsync(seriesId)
            ?? throw new KeyNotFoundException("常態性預約不存在");

        if (series.Status == SeriesStatus.Ended)
            throw new BusinessRuleException("此系列已結束，無法續約");

        // BR-07: 延長不超過 3 個月
        var newExpiry = series.ExpiresAt.AddMonths(dto.Months);
        var maxExpiry = LocalToday().AddMonths(3);
        if (newExpiry > maxExpiry)
            throw new BusinessRuleException($"續約後到期日不可超過 {maxExpiry}（3 個月上限）");

        series.ExpiresAt = newExpiry;
        series.Status = SeriesStatus.Active;
        series.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var created = await GenerateAppointmentsAsync(series, orgId, createdBy);

        await _audit.LogAsync("renew", "appointment_series", seriesId,
            $"續約常態性預約至 {newExpiry}, 產生 {created} 筆新預約");

        return await GetByIdAsync(seriesId);
    }

    public async Task<AppointmentSeriesResponseDto> EndAsync(int seriesId)
    {
        var series = await _db.AppointmentSeries.FindAsync(seriesId)
            ?? throw new KeyNotFoundException("常態性預約不存在");

        series.Status = SeriesStatus.Ended;
        series.UpdatedAt = DateTime.UtcNow;

        // 取消所有未來的 scheduled 預約
        var futureAppointments = await _db.Appointments
            .Include(a => a.ScheduleSlot)
            .Where(a => a.SeriesId == seriesId
                && a.Status == AppointmentStatus.Scheduled
                && a.ScheduleSlot.Date >= LocalToday())
            .ToListAsync();

        foreach (var apt in futureAppointments)
        {
            apt.Status = AppointmentStatus.Cancelled;
            apt.UpdatedAt = DateTime.UtcNow;

            apt.ScheduleSlot.Status = SlotStatus.Available;
            apt.ScheduleSlot.IsReservedHold = false;
            apt.ScheduleSlot.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        await _audit.LogAsync("end", "appointment_series", seriesId,
            $"結束常態性預約，取消 {futureAppointments.Count} 筆未來預約");

        return await GetByIdAsync(seriesId);
    }

    public async Task<List<AppointmentSeriesResponseDto>> GetPendingRenewalAsync()
    {
        // BR-10: 剩餘 ≤ 2 次的系列
        var today = LocalToday();
        var activeSeries = await _db.AppointmentSeries
            .Include(s => s.ClientProfile)
            .Include(s => s.TherapistUser)
            .Include(s => s.Appointments)
            .Where(s => s.Status == SeriesStatus.Active)
            .ToListAsync();

        return activeSeries
            .Where(s =>
            {
                var remaining = s.Appointments
                    .Count(a => a.Status == AppointmentStatus.Scheduled);
                return remaining <= 2;
            })
            .Select(s => MapToDto(s))
            .ToList();
    }

    // 觸發點 #5：由每日背景排程呼叫，跨機構通知心理師常態性預約即將到期（series_renewal）
    public async Task<int> NotifyPendingRenewalsAsync()
    {
        var activeSeries = await _db.AppointmentSeries
            .IgnoreQueryFilters()
            .Include(s => s.ClientProfile)
            .Include(s => s.TherapistUser)
            .Include(s => s.Appointments)
            .Where(s => s.Status == SeriesStatus.Active)
            .ToListAsync();

        var pending = activeSeries
            .Where(s => s.Appointments.Count(a => a.Status == AppointmentStatus.Scheduled) <= 2)
            .ToList();

        foreach (var s in pending)
        {
            var vars = new Dictionary<string, string>
            {
                ["client_name"] = s.ClientProfile?.Name ?? "",
                ["therapist_name"] = s.TherapistUser?.DisplayName ?? "",
            };
            await _notify.SendAsync(NotificationEventType.SeriesRenewal, s.TherapistUserId, Roles.Therapist, s.OrganizationId, vars);
        }

        return pending.Count;
    }

    private async Task<int> GenerateAppointmentsAsync(
        AppointmentSeries series, int orgId, int createdBy)
    {
        var today = LocalToday();
        var startDate = today > DateOnly.FromDateTime(series.CreatedAt) ? today : DateOnly.FromDateTime(series.CreatedAt);
        var created = 0;

        // 找到第一個符合 day_of_week 的日期
        var current = startDate;
        while ((int)current.DayOfWeek != series.DayOfWeek)
            current = current.AddDays(1);

        var increment = series.Frequency == SeriesFrequency.Biweekly ? 14 : 7;

        while (current <= series.ExpiresAt)
        {
            // 檢查是否已有此日期的預約
            var exists = await _db.Appointments
                .Include(a => a.ScheduleSlot)
                .AnyAsync(a => a.SeriesId == series.Id
                    && a.ScheduleSlot.Date == current
                    && a.Status != AppointmentStatus.Cancelled);

            if (!exists)
            {
                // 找到匹配的 slot
                var slot = await _db.ScheduleSlots
                    .FirstOrDefaultAsync(s =>
                        s.TherapistUserId == series.TherapistUserId
                        && s.Date == current
                        && s.StartTime == series.StartTime
                        && s.Status == SlotStatus.Available);

                // 個案在此時段是否已有重疊預約（可能來自其他心理師/系列），有則跳過該次
                var clientBusy = slot != null && await _appointments.HasClientTimeConflictAsync(
                    series.ClientProfileId, slot.Date, slot.StartTime, slot.EndTime);

                if (slot != null && !clientBusy)
                {
                    slot.Status = SlotStatus.Reserved;
                    slot.IsReservedHold = true;
                    slot.UpdatedAt = DateTime.UtcNow;

                    var appointment = new Appointment
                    {
                        OrganizationId = orgId,
                        ClientProfileId = series.ClientProfileId,
                        TherapistUserId = series.TherapistUserId,
                        RoomId = slot.RoomId,
                        ScheduleSlotId = slot.Id,
                        SeriesId = series.Id,
                        SessionType = AppointmentSessionType.Individual,
                        VisitType = AppointmentVisitType.FollowUp,
                        CreatedBy = createdBy,
                    };

                    _db.Appointments.Add(appointment);
                    created++;
                }
            }

            current = current.AddDays(increment);
        }

        if (created > 0)
            await _db.SaveChangesAsync();

        return created;
    }

    private async Task<AppointmentSeriesResponseDto> GetByIdAsync(int id)
    {
        var series = await _db.AppointmentSeries
            .Include(s => s.ClientProfile)
            .Include(s => s.TherapistUser)
            .Include(s => s.Appointments)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException("常態性預約不存在");

        return MapToDto(series);
    }

    // 依個案查詢常態性預約（可選 status 篩選）
    public async Task<List<AppointmentSeriesResponseDto>> GetByClientAsync(int clientProfileId, string? status)
    {
        var query = _db.AppointmentSeries
            .Include(s => s.ClientProfile)
            .Include(s => s.TherapistUser)
            .Include(s => s.Appointments)
            .Where(s => s.ClientProfileId == clientProfileId);

        if (!string.IsNullOrEmpty(status)) query = query.Where(s => s.Status == status);

        var list = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
        return list.Select(MapToDto).ToList();
    }

    private static AppointmentSeriesResponseDto MapToDto(AppointmentSeries s)
    {
        var remaining = s.Appointments?
            .Count(a => a.Status == AppointmentStatus.Scheduled) ?? 0;

        return new AppointmentSeriesResponseDto
        {
            Id = s.Id,
            ClientProfileId = s.ClientProfileId,
            ClientName = s.ClientProfile?.Name ?? "",
            TherapistUserId = s.TherapistUserId,
            TherapistName = s.TherapistUser?.DisplayName ?? "",
            DayOfWeek = s.DayOfWeek,
            StartTime = s.StartTime.ToString("HH:mm"),
            Frequency = s.Frequency,
            Status = s.Status,
            ExpiresAt = s.ExpiresAt.ToString("yyyy-MM-dd"),
            TotalAppointments = s.Appointments?.Count ?? 0,
            RemainingAppointments = remaining,
            CreatedAt = s.CreatedAt,
        };
    }
}
