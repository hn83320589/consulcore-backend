using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace consulcore_backend.Models;

[Table("notification_templates")]
public class NotificationTemplate
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("organization_id")]
    public int OrganizationId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("event_type")]
    public string EventType { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("channel")]
    public string Channel { get; set; } = NotificationChannel.InApp;

    [Required]
    [MaxLength(200)]
    [Column("subject")]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [Column("body_template")]
    public string BodyTemplate { get; set; } = string.Empty;

    [Column("is_required")]
    public bool IsRequired { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("OrganizationId")]
    public Organization Organization { get; set; } = null!;
}

public static class NotificationEventType
{
    public const string AccountCreated = "account_created";
    public const string AppointmentCreated = "appointment_created";
    public const string AppointmentCancelled = "appointment_cancelled";
    public const string AppointmentReminder = "appointment_reminder";
    public const string TherapistLeave = "therapist_leave";
    public const string SeriesRenewal = "series_renewal";
    public const string PackageLow = "package_low";
    public const string PackageExpiring = "package_expiring";
}

public static class NotificationChannel
{
    public const string Email = "email";
    public const string Sms = "sms";
    public const string InApp = "in_app";
}
