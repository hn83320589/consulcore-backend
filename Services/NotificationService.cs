using consulcore_backend.Data;
using consulcore_backend.DTOs.Notification;
using consulcore_backend.Exceptions;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace consulcore_backend.Services;

public class NotificationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(AppDbContext db, ILogger<NotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SendAsync(string eventType, int recipientId, string recipientType,
        int orgId, Dictionary<string, string>? templateVars = null)
    {
        var templates = await _db.NotificationTemplates
            .IgnoreQueryFilters()
            .Where(t => t.OrganizationId == orgId && t.EventType == eventType && t.IsActive)
            .ToListAsync();

        if (templates.Count == 0) return;

        // 查用戶偏好
        var prefs = await _db.NotificationPreferences
            .IgnoreQueryFilters()
            .Where(p => p.UserId == recipientId && p.EventType == eventType)
            .FirstOrDefaultAsync();

        foreach (var template in templates)
        {
            // BR-12: 必要通知不可關閉
            var isEnabled = template.IsRequired || IsChannelEnabled(prefs, template.Channel);
            if (!isEnabled) continue;

            var body = ReplaceVars(template.BodyTemplate, templateVars);
            var subject = ReplaceVars(template.Subject, templateVars);

            var log = new NotificationLog
            {
                OrganizationId = orgId,
                TemplateId = template.Id,
                RecipientId = recipientId,
                RecipientType = recipientType,
                Channel = template.Channel,
                Subject = subject,
                Body = body,
                Status = NotificationStatus.Queued,
            };
            _db.NotificationLogs.Add(log);
            await _db.SaveChangesAsync();

            // 發送
            try
            {
                switch (template.Channel)
                {
                    case NotificationChannel.InApp:
                        log.Status = NotificationStatus.Sent;
                        log.SentAt = DateTime.UtcNow;
                        break;
                    case NotificationChannel.Email:
                        // TODO: 接 SMTP
                        _logger.LogInformation("[Email Mock] To userId={RecipientId}: {Subject}", recipientId, subject);
                        log.Status = NotificationStatus.Sent;
                        log.SentAt = DateTime.UtcNow;
                        break;
                    case NotificationChannel.Sms:
                        // TODO: 接 SMS API
                        _logger.LogInformation("[SMS Mock] To userId={RecipientId}: {Body}", recipientId, body);
                        log.Status = NotificationStatus.Sent;
                        log.SentAt = DateTime.UtcNow;
                        break;
                }
            }
            catch (Exception ex)
            {
                log.Status = NotificationStatus.Failed;
                log.ErrorMessage = ex.Message;
                _logger.LogError(ex, "通知發送失敗 channel={Channel}", template.Channel);
            }
            await _db.SaveChangesAsync();
        }
    }

    // === Template CRUD ===

    public async Task<List<NotificationTemplateResponseDto>> GetAllTemplatesAsync()
    {
        return await _db.NotificationTemplates
            .OrderBy(t => t.EventType).ThenBy(t => t.Channel)
            .Select(t => MapTemplate(t))
            .ToListAsync();
    }

    public async Task<NotificationTemplateResponseDto> CreateTemplateAsync(
        CreateNotificationTemplateDto dto, int orgId)
    {
        var t = new NotificationTemplate
        {
            OrganizationId = orgId, EventType = dto.EventType, Channel = dto.Channel,
            Subject = dto.Subject, BodyTemplate = dto.BodyTemplate, IsRequired = dto.IsRequired,
        };
        _db.NotificationTemplates.Add(t);
        await _db.SaveChangesAsync();
        return MapTemplate(t);
    }

    public async Task<NotificationTemplateResponseDto> UpdateTemplateAsync(
        int id, UpdateNotificationTemplateDto dto)
    {
        var t = await _db.NotificationTemplates.FindAsync(id)
            ?? throw new KeyNotFoundException("通知範本不存在");
        if (dto.Subject != null) t.Subject = dto.Subject;
        if (dto.BodyTemplate != null) t.BodyTemplate = dto.BodyTemplate;
        if (dto.IsActive.HasValue) t.IsActive = dto.IsActive.Value;
        t.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return MapTemplate(t);
    }

    private static NotificationTemplateResponseDto MapTemplate(NotificationTemplate t) => new()
    {
        Id = t.Id, EventType = t.EventType, Channel = t.Channel,
        Subject = t.Subject, BodyTemplate = t.BodyTemplate,
        IsRequired = t.IsRequired, IsActive = t.IsActive,
    };

    public async Task DeleteTemplateAsync(int id)
    {
        var t = await _db.NotificationTemplates.FindAsync(id)
            ?? throw new KeyNotFoundException("通知範本不存在");
        _db.NotificationTemplates.Remove(t);
        await _db.SaveChangesAsync();
    }

    // === User notifications ===

    public async Task<List<NotificationLogResponseDto>> GetByUserAsync(int userId)
    {
        return await _db.NotificationLogs
            .Where(n => n.RecipientId == userId && n.Channel == NotificationChannel.InApp)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationLogResponseDto
            {
                Id = n.Id, Channel = n.Channel, Subject = n.Subject, Body = n.Body,
                Status = n.Status, SentAt = n.SentAt, ReadAt = n.ReadAt, CreatedAt = n.CreatedAt,
            }).ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _db.NotificationLogs
            .CountAsync(n => n.RecipientId == userId
                && n.Channel == NotificationChannel.InApp
                && n.Status == NotificationStatus.Sent);
    }

    public async Task MarkAsReadAsync(int notificationId, int userId)
    {
        // 只能標記自己的通知；別人的通知一律當作不存在（不透露該通知存在）
        var n = await _db.NotificationLogs
            .FirstOrDefaultAsync(x => x.Id == notificationId && x.RecipientId == userId)
            ?? throw new KeyNotFoundException("通知不存在");
        n.Status = NotificationStatus.Read;
        n.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var unread = await _db.NotificationLogs
            .Where(n => n.RecipientId == userId
                && n.Channel == NotificationChannel.InApp
                && n.Status == NotificationStatus.Sent)
            .ToListAsync();
        foreach (var n in unread)
        {
            n.Status = NotificationStatus.Read;
            n.ReadAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
    }

    // === Preferences ===

    public async Task<List<PreferenceResponseDto>> GetPreferencesAsync(int userId)
    {
        var prefs = await _db.NotificationPreferences
            .IgnoreQueryFilters()
            .Where(p => p.UserId == userId)
            .ToListAsync();

        return prefs.Select(p => new PreferenceResponseDto
        {
            EventType = p.EventType, EmailEnabled = p.EmailEnabled,
            SmsEnabled = p.SmsEnabled, InAppEnabled = p.InAppEnabled,
        }).ToList();
    }

    public async Task UpdatePreferenceAsync(int userId, UpdatePreferenceDto dto)
    {
        // BR-12: 必要通知不可關閉。若該 event_type 在使用者機構內有 IsRequired 範本，
        // 則強制三管道保持啟用，避免偏好與實際發送行為不一致。
        var orgId = await _db.Users
            .IgnoreQueryFilters()
            .Where(u => u.Id == userId)
            .Select(u => u.OrganizationId)
            .FirstOrDefaultAsync();

        var isRequired = await _db.NotificationTemplates
            .IgnoreQueryFilters()
            .AnyAsync(t => t.OrganizationId == orgId && t.EventType == dto.EventType && t.IsRequired);

        if (isRequired && (!dto.EmailEnabled || !dto.SmsEnabled || !dto.InAppEnabled))
            throw new BusinessRuleException("此為必要通知，無法關閉");

        var pref = await _db.NotificationPreferences
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.EventType == dto.EventType);

        if (pref == null)
        {
            pref = new NotificationPreference
            {
                UserId = userId, EventType = dto.EventType,
                EmailEnabled = dto.EmailEnabled, SmsEnabled = dto.SmsEnabled,
                InAppEnabled = dto.InAppEnabled,
            };
            _db.NotificationPreferences.Add(pref);
        }
        else
        {
            pref.EmailEnabled = dto.EmailEnabled;
            pref.SmsEnabled = dto.SmsEnabled;
            pref.InAppEnabled = dto.InAppEnabled;
        }
        await _db.SaveChangesAsync();
    }

    private static bool IsChannelEnabled(NotificationPreference? pref, string channel)
    {
        if (pref == null) return true; // default all enabled
        return channel switch
        {
            NotificationChannel.Email => pref.EmailEnabled,
            NotificationChannel.Sms => pref.SmsEnabled,
            NotificationChannel.InApp => pref.InAppEnabled,
            _ => true,
        };
    }

    private static string ReplaceVars(string template, Dictionary<string, string>? vars)
    {
        if (vars == null) return template;
        foreach (var (key, value) in vars)
            template = template.Replace($"{{{{{key}}}}}", value);
        return template;
    }
}
