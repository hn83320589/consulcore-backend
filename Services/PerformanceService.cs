using consulcore_backend.Authorization;
using consulcore_backend.Data;
using consulcore_backend.DTOs.PerformanceReview;
using consulcore_backend.Exceptions;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace consulcore_backend.Services;

public class PerformanceService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;
    private readonly ClinicClock _clock;

    public PerformanceService(AppDbContext db, IAuditService audit, ClinicClock clock)
    {
        _db = db;
        _audit = audit;
        _clock = clock;
    }

    public async Task<PerformanceReviewResponseDto> CreateAsync(
        CreatePerformanceReviewDto dto, int orgId, int reviewerId)
    {
        var review = new Models.PerformanceReview
        {
            OrganizationId = orgId, TherapistUserId = dto.TherapistUserId, ReviewerId = reviewerId,
            Period = dto.Period, ProfessionalScore = dto.ProfessionalScore,
            ClientFeedbackScore = dto.ClientFeedbackScore, TeamworkScore = dto.TeamworkScore,
            OverallScore = dto.OverallScore, Comment = dto.Comment,
        };
        _db.PerformanceReviews.Add(review);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("create", "performance_review", review.Id, $"建立考核 {dto.Period}");
        return await GetByIdAsync(review.Id, true);
    }

    public async Task<PerformanceReviewResponseDto> UpdateAsync(
        int id, UpdatePerformanceReviewDto dto)
    {
        var r = await _db.PerformanceReviews.FindAsync(id)
            ?? throw new KeyNotFoundException("考核不存在");
        if (dto.ProfessionalScore.HasValue) r.ProfessionalScore = dto.ProfessionalScore.Value;
        if (dto.ClientFeedbackScore.HasValue) r.ClientFeedbackScore = dto.ClientFeedbackScore.Value;
        if (dto.TeamworkScore.HasValue) r.TeamworkScore = dto.TeamworkScore.Value;
        if (dto.OverallScore.HasValue) r.OverallScore = dto.OverallScore.Value;
        if (dto.Comment != null) r.Comment = dto.Comment;
        r.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("update", "performance_review", id, "更新考核");
        return await GetByIdAsync(id, true);
    }

    public async Task DeleteAsync(int id)
    {
        var r = await _db.PerformanceReviews.FindAsync(id)
            ?? throw new KeyNotFoundException("考核不存在");
        _db.PerformanceReviews.Remove(r);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("delete", "performance_review", id, "刪除考核");
    }

    public async Task<PerformanceReviewResponseDto> SetVisibilityAsync(int id, SetVisibilityDto dto, int userId)
    {
        var r = await _db.PerformanceReviews.FindAsync(id)
            ?? throw new KeyNotFoundException("考核不存在");
        r.Visibility = dto.Visibility;
        r.VisibleFrom = dto.VisibleFrom;
        r.VisibleUntil = dto.VisibleUntil;
        if (dto.Visibility == ReviewVisibility.Visible)
        {
            r.OpenedBy = userId;
            r.OpenedAt = DateTime.UtcNow;
        }
        r.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("set_visibility", "performance_review", id,
            $"設定可見性 {dto.Visibility}");
        return await GetByIdAsync(id, true);
    }

    // BR-17: 到期回 hidden
    // 系統維護任務：跨機構掃描考核可見性排程，故 IgnoreQueryFilters（背景執行無 org context）
    public async Task<int> CheckVisibilityScheduleAsync()
    {
        var now = _clock.UtcNow;
        var changed = 0;

        // 到期：VisibleUntil 已過 → 收回
        var expired = await _db.PerformanceReviews
            .IgnoreQueryFilters()
            .Where(r => r.Visibility == ReviewVisibility.Visible
                && r.VisibleUntil.HasValue && r.VisibleUntil < now)
            .ToListAsync();
        foreach (var r in expired) r.Visibility = ReviewVisibility.Hidden;
        changed += expired.Count;

        // 到時開放：VisibleFrom 已到且尚未過期 → 開放
        var toOpen = await _db.PerformanceReviews
            .IgnoreQueryFilters()
            .Where(r => r.Visibility == ReviewVisibility.Hidden
                && r.VisibleFrom.HasValue && r.VisibleFrom <= now
                && (!r.VisibleUntil.HasValue || r.VisibleUntil > now))
            .ToListAsync();
        foreach (var r in toOpen) r.Visibility = ReviewVisibility.Visible;
        changed += toOpen.Count;

        await _db.SaveChangesAsync();

        foreach (var r in expired)
            await _audit.LogSystemAsync(r.OrganizationId, "visibility_close", "performance_review", r.Id,
                $"考核 {r.Period} 到期收回");
        foreach (var r in toOpen)
            await _audit.LogSystemAsync(r.OrganizationId, "visibility_open", "performance_review", r.Id,
                $"考核 {r.Period} 依排程開放");

        return changed;
    }

    // BR-18: 量化指標心理師可見，質性考核僅 Owner
    public async Task<List<PerformanceReviewResponseDto>> GetByTherapistAsync(int therapistUserId)
    {
        return await _db.PerformanceReviews
            .Include(r => r.TherapistUser).Include(r => r.Reviewer)
            .Where(r => r.TherapistUserId == therapistUserId && r.Visibility == ReviewVisibility.Visible)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new PerformanceReviewResponseDto
            {
                Id = r.Id, TherapistUserId = r.TherapistUserId,
                TherapistName = r.TherapistUser.DisplayName, ReviewerId = r.ReviewerId,
                ReviewerName = r.Reviewer.DisplayName, Period = r.Period,
                ProfessionalScore = r.ProfessionalScore, ClientFeedbackScore = r.ClientFeedbackScore,
                TeamworkScore = r.TeamworkScore, OverallScore = r.OverallScore,
                Comment = null, // BR-18: 質性考核僅 Owner
                Visibility = r.Visibility, VisibleFrom = r.VisibleFrom,
                VisibleUntil = r.VisibleUntil, CreatedAt = r.CreatedAt,
            }).ToListAsync();
    }

    public async Task<List<PerformanceReviewResponseDto>> GetAllReviewsAsync()
    {
        var reviews = await _db.PerformanceReviews.AsNoTracking()
            .Include(r => r.TherapistUser).Include(r => r.Reviewer)
            .OrderByDescending(r => r.CreatedAt).ToListAsync();
        return reviews.Select(r => MapToDto(r, true)).ToList();
    }

    public async Task<TherapistMetricsDto> GetMetricsAsync(int therapistUserId, DateOnly startDate, DateOnly endDate)
    {
        var therapistName = await _db.Users
            .Where(u => u.Id == therapistUserId)
            .Select(u => u.DisplayName)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("心理師不存在");

        // 只查計算需要的欄位：載入整筆晤談紀錄會解密每筆 SOAP / 自由文字內容，指標用不到
        var appointments = await _db.Appointments
            .Where(a => a.TherapistUserId == therapistUserId
                && a.ScheduleSlot.Date >= startDate && a.ScheduleSlot.Date <= endDate)
            .Select(a => new { a.Id, a.Status, a.ScheduleSlot.Date, a.ScheduleSlot.StartTime })
            .ToListAsync();

        var completed = appointments.Count(a => a.Status == AppointmentStatus.Completed);
        var noShow = appointments.Count(a => a.Status == AppointmentStatus.NoShow);
        // 分母只算「該出席的」：已排定的尚未發生、已取消的是提前取消，都不算心理師未完成；
        // 也避免查詢區間延伸到未來時，兩個畫面的比率不一致
        var totalApts = completed + noShow;

        var notes = await _db.SessionNotes
            .Where(n => n.TherapistUserId == therapistUserId
                && n.CreatedAt >= startDate.ToDateTime(TimeOnly.MinValue)
                && n.CreatedAt <= endDate.ToDateTime(TimeOnly.MaxValue))
            .Select(n => new { n.AppointmentId, n.IsLocked, n.CreatedAt })
            .ToListAsync();

        var totalNotes = notes.Count;
        var lockedNotes = notes.Count(n => n.IsLocked);
        // 紀錄及時性：晤談紀錄建立時間 與「晤談實際發生時間」（slot 日期+起始時間）的時差
        var avgDelay = notes.Where(n => n.AppointmentId.HasValue).Select(n =>
        {
            var apt = appointments.FirstOrDefault(a => a.Id == n.AppointmentId);
            if (apt == null) return 0;
            var sessionTime = apt.Date.ToDateTime(apt.StartTime);
            return (n.CreatedAt - sessionTime).TotalHours;
        }).DefaultIfEmpty(0).Average();

        var activeClients = await _db.TherapistClients
            .CountAsync(tc => tc.TherapistUserId == therapistUserId
                && tc.Status == TherapistClientStatus.Active);
        var longTermClients = await _db.TherapistClients
            .CountAsync(tc => tc.TherapistUserId == therapistUserId
                && tc.Status == TherapistClientStatus.Active
                && tc.CreatedAt <= _clock.UtcNow.AddMonths(-3));

        var appointmentIds = appointments.Select(a => a.Id).ToList();
        var paymentStatuses = await _db.PaymentRecords
            .Where(r => r.AppointmentId.HasValue && appointmentIds.Contains(r.AppointmentId.Value))
            .Select(r => r.Status)
            .ToListAsync();
        var paidPayments = paymentStatuses.Count(s => s == PaymentStatus.Paid);

        var sessionHours = completed * 50.0m / 60; // assume 50min sessions

        return new TherapistMetricsDto
        {
            TherapistUserId = therapistUserId,
            TherapistName = therapistName,
            AppointmentCompletionRate = totalApts > 0 ? Math.Round((decimal)completed / totalApts * 100, 1) : 0,
            NoShowRate = totalApts > 0 ? Math.Round((decimal)noShow / totalApts * 100, 1) : 0,
            AvgNoteDelayHours = Math.Round((decimal)avgDelay, 1),
            NoteLockRate = totalNotes > 0 ? Math.Round((decimal)lockedNotes / totalNotes * 100, 1) : 0,
            ClientRetentionRate = activeClients > 0 ? Math.Round((decimal)longTermClients / activeClients * 100, 1) : 0,
            MonthlySessionCount = completed,
            MonthlySessionHours = sessionHours,
            PaymentCompletionRate = paymentStatuses.Count > 0 ? Math.Round((decimal)paidPayments / paymentStatuses.Count * 100, 1) : 0,
        };
    }

    public async Task<List<TherapistMetricsDto>> GetMetricsComparisonAsync(DateOnly startDate, DateOnly endDate)
    {
        var therapistIds = await _db.Users
            .Where(u => !u.IsDeleted && u.UserRoles.Any(ur => ur.Role.Name == Roles.Therapist))
            .Select(u => u.Id).ToListAsync();

        var results = new List<TherapistMetricsDto>();
        foreach (var id in therapistIds)
            results.Add(await GetMetricsAsync(id, startDate, endDate));
        return results;
    }

    // 單一指標隨時間的趨勢（目前以月為區間），逐月計算指定指標值
    public async Task<MetricsTrendDto> GetMetricsTrendAsync(
        int therapistUserId, string metric, DateOnly startDate, DateOnly endDate)
    {
        var points = new List<MetricPointDto>();

        // 從 startDate 所在月的月初，逐月推進到 endDate 所在月
        var cursor = new DateOnly(startDate.Year, startDate.Month, 1);
        var last = new DateOnly(endDate.Year, endDate.Month, 1);

        while (cursor <= last)
        {
            var monthStart = cursor;
            var monthEnd = cursor.AddMonths(1).AddDays(-1);
            var m = await GetMetricsAsync(therapistUserId, monthStart, monthEnd);
            points.Add(new MetricPointDto
            {
                Period = cursor.ToString("yyyy-MM"),
                Value = ExtractMetric(m, metric),
            });
            cursor = cursor.AddMonths(1);
        }

        return new MetricsTrendDto { Metric = metric, Points = points };
    }

    // 指標鍵（camelCase）→ TherapistMetricsDto 欄位值
    private static decimal ExtractMetric(TherapistMetricsDto m, string metric) => metric switch
    {
        "appointmentCompletionRate" => m.AppointmentCompletionRate,
        "noShowRate" => m.NoShowRate,
        "avgNoteDelayHours" => m.AvgNoteDelayHours,
        "noteLockRate" => m.NoteLockRate,
        "clientRetentionRate" => m.ClientRetentionRate,
        "monthlySessionCount" => m.MonthlySessionCount,
        "monthlySessionHours" => m.MonthlySessionHours,
        "paymentCompletionRate" => m.PaymentCompletionRate,
        _ => throw new BusinessRuleException($"未知的指標鍵：{metric}"),
    };

    private async Task<PerformanceReviewResponseDto> GetByIdAsync(int id, bool includeComment)
    {
        var r = await _db.PerformanceReviews
            .Include(r => r.TherapistUser).Include(r => r.Reviewer)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException("考核不存在");
        return MapToDto(r, includeComment);
    }

    private static PerformanceReviewResponseDto MapToDto(Models.PerformanceReview r, bool includeComment) => new()
    {
        Id = r.Id, TherapistUserId = r.TherapistUserId,
        TherapistName = r.TherapistUser?.DisplayName ?? "", ReviewerId = r.ReviewerId,
        ReviewerName = r.Reviewer?.DisplayName ?? "", Period = r.Period,
        ProfessionalScore = r.ProfessionalScore, ClientFeedbackScore = r.ClientFeedbackScore,
        TeamworkScore = r.TeamworkScore, OverallScore = r.OverallScore,
        Comment = includeComment ? r.Comment : null,
        Visibility = r.Visibility, VisibleFrom = r.VisibleFrom,
        VisibleUntil = r.VisibleUntil, CreatedAt = r.CreatedAt,
    };
}
