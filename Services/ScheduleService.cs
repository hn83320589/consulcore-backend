using consulcore_backend.Data;
using consulcore_backend.DTOs.ScheduleSlot;
using consulcore_backend.DTOs.ScheduleTemplate;
using consulcore_backend.Exceptions;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace consulcore_backend.Services;

public class ScheduleService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;
    private readonly NotificationService _notify;

    public ScheduleService(AppDbContext db, IAuditService audit, NotificationService notify)
    {
        _db = db;
        _audit = audit;
        _notify = notify;
    }

    // 台灣時區固定 UTC+8（無日光節約），以此計算「今天」避免 UTC 邊界日誤判
    private static DateOnly LocalToday() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(8));

    // === Template CRUD ===

    public async Task<List<ScheduleTemplateResponseDto>> GetAllTemplatesAsync()
    {
        return await _db.ScheduleTemplates
            .Include(t => t.TherapistUser)
            .OrderBy(t => t.TherapistUserId)
            .ThenBy(t => t.DayOfWeek)
            .Select(t => MapTemplateToDto(t))
            .ToListAsync();
    }

    public async Task<ScheduleTemplateResponseDto> GetTemplateByIdAsync(int id)
    {
        var template = await _db.ScheduleTemplates
            .Include(t => t.TherapistUser)
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new KeyNotFoundException("班表範本不存在");

        return MapTemplateToDto(template);
    }

    public async Task<ScheduleTemplateResponseDto> CreateTemplateAsync(CreateScheduleTemplateDto dto, int orgId)
    {
        var template = new ScheduleTemplate
        {
            OrganizationId = orgId,
            TherapistUserId = dto.TherapistUserId,
            DayOfWeek = dto.DayOfWeek,
            StartTime = TimeOnly.Parse(dto.StartTime),
            EndTime = TimeOnly.Parse(dto.EndTime),
            SlotDuration = dto.SlotDuration,
        };

        _db.ScheduleTemplates.Add(template);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("create", "schedule_template", template.Id,
            $"建立班表範本 (therapist={dto.TherapistUserId}, day={dto.DayOfWeek})");

        return await GetTemplateByIdAsync(template.Id);
    }

    public async Task<ScheduleTemplateResponseDto> UpdateTemplateAsync(int id, UpdateScheduleTemplateDto dto)
    {
        var template = await _db.ScheduleTemplates.FindAsync(id)
            ?? throw new KeyNotFoundException("班表範本不存在");

        if (dto.StartTime != null) template.StartTime = TimeOnly.Parse(dto.StartTime);
        if (dto.EndTime != null) template.EndTime = TimeOnly.Parse(dto.EndTime);
        if (dto.SlotDuration.HasValue) template.SlotDuration = dto.SlotDuration.Value;
        if (dto.IsActive.HasValue) template.IsActive = dto.IsActive.Value;
        template.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.LogAsync("update", "schedule_template", id, "更新班表範本");

        return await GetTemplateByIdAsync(id);
    }

    public async Task DeleteTemplateAsync(int id)
    {
        var template = await _db.ScheduleTemplates.FindAsync(id)
            ?? throw new KeyNotFoundException("班表範本不存在");

        _db.ScheduleTemplates.Remove(template);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("delete", "schedule_template", id, "刪除班表範本");
    }

    // === Slot Operations ===

    public async Task<ExpandResultDto> ExpandScheduleAsync(DateOnly startDate, DateOnly endDate, int orgId)
    {
        // BR-05: 不可調整過去日期
        var today = LocalToday();
        if (startDate < today)
            startDate = today;

        if (endDate < startDate)
            throw new BusinessRuleException("結束日期不可早於開始日期");

        var templates = await _db.ScheduleTemplates
            .Where(t => t.IsActive)
            .ToListAsync();

        if (templates.Count == 0)
            return new ExpandResultDto { Warnings = ["沒有啟用中的班表範本"] };

        // 取得心理師的診間映射
        var therapistRooms = await _db.TherapistRooms
            .GroupBy(tr => tr.TherapistUserId)
            .ToDictionaryAsync(g => g.Key, g => g.First().RoomId);

        // 取得已存在的 slots（用於不覆蓋判斷）
        var existingSlots = await _db.ScheduleSlots
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .Select(s => new { s.TherapistUserId, s.Date, s.StartTime })
            .ToListAsync();

        var existingSet = existingSlots
            .Select(s => $"{s.TherapistUserId}_{s.Date}_{s.StartTime}")
            .ToHashSet();

        var created = 0;
        var skipped = 0;
        var newSlots = new List<ScheduleSlot>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var dayOfWeek = (int)date.DayOfWeek;
            var dayTemplates = templates.Where(t => t.DayOfWeek == dayOfWeek);

            foreach (var template in dayTemplates)
            {
                var currentTime = template.StartTime;
                while (currentTime.AddMinutes(template.SlotDuration) <= template.EndTime)
                {
                    var slotEnd = currentTime.AddMinutes(template.SlotDuration);
                    var key = $"{template.TherapistUserId}_{date}_{currentTime}";

                    if (existingSet.Contains(key))
                    {
                        skipped++;
                    }
                    else
                    {
                        // BR-03: 從 TherapistRoom 自動分配診間
                        therapistRooms.TryGetValue(template.TherapistUserId, out var roomId);

                        newSlots.Add(new ScheduleSlot
                        {
                            OrganizationId = orgId,
                            TherapistUserId = template.TherapistUserId,
                            RoomId = roomId > 0 ? roomId : null,
                            Date = date,
                            StartTime = currentTime,
                            EndTime = slotEnd,
                        });
                        created++;
                    }

                    currentTime = slotEnd;
                }
            }
        }

        if (newSlots.Count > 0)
        {
            _db.ScheduleSlots.AddRange(newSlots);
            await _db.SaveChangesAsync();
        }

        // BR-02: 衝突檢查 — 同時段心理師數 > 啟用中診間數
        var warnings = await CheckConflictsAsync(startDate, endDate);

        await _audit.LogAsync("expand", "schedule_slot", 0,
            $"展班 {startDate} ~ {endDate}: 新增 {created}, 跳過 {skipped}");

        return new ExpandResultDto
        {
            CreatedCount = created,
            SkippedCount = skipped,
            Warnings = warnings,
        };
    }

    public async Task<ExpandResultDto> ManualExpandAsync(int year, int month, int orgId)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        return await ExpandScheduleAsync(startDate, endDate, orgId);
    }

    public async Task<ScheduleSlotResponseDto> BlockSlotAsync(int slotId, BlockSlotDto dto)
    {
        var slot = await _db.ScheduleSlots.FindAsync(slotId)
            ?? throw new KeyNotFoundException("時段不存在");

        // BR-05
        var today = LocalToday();
        if (slot.Date < today)
            throw new BusinessRuleException("不可調整過去日期的時段");

        // 心理師請假封鎖時段：若已有預約，取消該預約並通知受影響個案（觸發點 #4 therapist_leave）
        var affected = await _db.Appointments
            .Where(a => a.ScheduleSlotId == slotId && a.Status == AppointmentStatus.Scheduled)
            .ToListAsync();

        slot.Status = SlotStatus.Blocked;
        slot.IsReservedHold = false;
        slot.Note = dto.Note;
        slot.UpdatedAt = DateTime.UtcNow;

        foreach (var apt in affected)
        {
            apt.Status = AppointmentStatus.Cancelled;
            apt.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync("block", "schedule_slot", slotId,
            $"封鎖時段 {slot.Date} {slot.StartTime}（取消 {affected.Count} 筆預約）");

        // 通知受影響個案：therapist_leave
        foreach (var apt in affected)
        {
            var client = await _db.ClientProfiles.FindAsync(apt.ClientProfileId);
            var therapist = await _db.Users.FindAsync(apt.TherapistUserId);
            var vars = new Dictionary<string, string>
            {
                ["client_name"] = client?.Name ?? "",
                ["therapist_name"] = therapist?.DisplayName ?? "",
                ["date"] = slot.Date.ToString("yyyy-MM-dd"),
                ["time"] = slot.StartTime.ToString("HH:mm"),
            };
            if (client?.UserId != null)
                await _notify.SendAsync(NotificationEventType.TherapistLeave, client.UserId.Value, "client", slot.OrganizationId, vars);
        }

        return await GetSlotByIdAsync(slotId);
    }

    public async Task<ScheduleSlotResponseDto> UnblockSlotAsync(int slotId)
    {
        var slot = await _db.ScheduleSlots.FindAsync(slotId)
            ?? throw new KeyNotFoundException("時段不存在");

        var today = LocalToday();
        if (slot.Date < today)
            throw new BusinessRuleException("不可調整過去日期的時段");

        if (slot.Status != SlotStatus.Blocked)
            throw new BusinessRuleException("此時段非封鎖狀態");

        slot.Status = SlotStatus.Available;
        slot.Note = null;
        slot.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.LogAsync("unblock", "schedule_slot", slotId, $"解除封鎖時段 {slot.Date} {slot.StartTime}");

        return await GetSlotByIdAsync(slotId);
    }

    public async Task<List<ScheduleSlotResponseDto>> GetSlotsAsync(
        DateOnly? startDate, DateOnly? endDate, int? therapistUserId)
    {
        var query = _db.ScheduleSlots
            .Include(s => s.TherapistUser)
            .Include(s => s.Room)
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(s => s.Date >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(s => s.Date <= endDate.Value);
        if (therapistUserId.HasValue)
            query = query.Where(s => s.TherapistUserId == therapistUserId.Value);

        return await query
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .Select(s => MapSlotToDto(s))
            .ToListAsync();
    }

    private async Task<ScheduleSlotResponseDto> GetSlotByIdAsync(int id)
    {
        var slot = await _db.ScheduleSlots
            .Include(s => s.TherapistUser)
            .Include(s => s.Room)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException("時段不存在");

        return MapSlotToDto(slot);
    }

    private async Task<List<string>> CheckConflictsAsync(DateOnly startDate, DateOnly endDate)
    {
        var warnings = new List<string>();
        var activeRoomCount = await _db.Rooms.CountAsync(r => r.IsActive);

        if (activeRoomCount == 0)
        {
            warnings.Add("目前沒有啟用中的診間");
            return warnings;
        }

        // 佔用診間的時段：Available 或 Reserved（Blocked/Cancelled 不佔用）
        var slots = await _db.ScheduleSlots
            .Where(s => s.Date >= startDate && s.Date <= endDate
                && (s.Status == SlotStatus.Available || s.Status == SlotStatus.Reserved))
            .Select(s => new { s.Date, s.StartTime, s.EndTime })
            .ToListAsync();

        // 以掃描線計算每日「同時進行中時段」的峰值（區間重疊），超過診間數即警告
        foreach (var dayGroup in slots.GroupBy(s => s.Date))
        {
            var events = new List<(TimeOnly Time, int Delta)>();
            foreach (var s in dayGroup)
            {
                events.Add((s.StartTime, +1));
                events.Add((s.EndTime, -1));
            }

            // 同一時刻先處理結束(-1)再處理開始(+1)，讓「前一段結束=後一段開始」不算重疊
            events.Sort((a, b) => a.Time != b.Time
                ? a.Time.CompareTo(b.Time)
                : a.Delta.CompareTo(b.Delta));

            var concurrent = 0;
            var peak = 0;
            var peakTime = dayGroup.Min(s => s.StartTime);
            foreach (var (time, delta) in events)
            {
                concurrent += delta;
                if (concurrent > peak)
                {
                    peak = concurrent;
                    peakTime = time;
                }
            }

            if (peak > activeRoomCount)
            {
                warnings.Add(
                    $"{dayGroup.Key} {peakTime:HH:mm} 起最多 {peak} 個時段重疊，" +
                    $"但只有 {activeRoomCount} 間診間");
            }
        }

        return warnings;
    }

    private static ScheduleTemplateResponseDto MapTemplateToDto(ScheduleTemplate t)
    {
        return new ScheduleTemplateResponseDto
        {
            Id = t.Id,
            TherapistUserId = t.TherapistUserId,
            TherapistName = t.TherapistUser?.DisplayName ?? "",
            DayOfWeek = t.DayOfWeek,
            StartTime = t.StartTime.ToString("HH:mm"),
            EndTime = t.EndTime.ToString("HH:mm"),
            SlotDuration = t.SlotDuration,
            IsActive = t.IsActive,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
        };
    }

    private static ScheduleSlotResponseDto MapSlotToDto(ScheduleSlot s)
    {
        return new ScheduleSlotResponseDto
        {
            Id = s.Id,
            TherapistUserId = s.TherapistUserId,
            TherapistName = s.TherapistUser?.DisplayName ?? "",
            RoomId = s.RoomId,
            RoomName = s.Room?.Name,
            Date = s.Date.ToString("yyyy-MM-dd"),
            StartTime = s.StartTime.ToString("HH:mm"),
            EndTime = s.EndTime.ToString("HH:mm"),
            Status = s.Status,
            Note = s.Note,
        };
    }
}
