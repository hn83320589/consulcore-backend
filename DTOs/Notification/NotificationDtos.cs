using System.ComponentModel.DataAnnotations;

namespace consulcore_backend.DTOs.Notification;

public class CreateNotificationTemplateDto
{
    [Required] [MaxLength(50)] public string EventType { get; set; } = string.Empty;
    [MaxLength(20)] public string Channel { get; set; } = "in_app";
    [Required] [MaxLength(200)] public string Subject { get; set; } = string.Empty;
    [Required] public string BodyTemplate { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
}

public class UpdateNotificationTemplateDto
{
    public string? Subject { get; set; }
    public string? BodyTemplate { get; set; }
    public bool? IsActive { get; set; }
}

public class NotificationTemplateResponseDto
{
    public int Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; }
}

public class NotificationLogResponseDto
{
    public int Id { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdatePreferenceDto
{
    [Required] public string EventType { get; set; } = string.Empty;
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = true;
    public bool InAppEnabled { get; set; } = true;
}

public class PreferenceResponseDto
{
    public string EventType { get; set; } = string.Empty;
    public bool EmailEnabled { get; set; }
    public bool SmsEnabled { get; set; }
    public bool InAppEnabled { get; set; }
}
