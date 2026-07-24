using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace consulcore_backend.Models;

[Table("notification_logs")]
public class NotificationLog
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("organization_id")]
    public int OrganizationId { get; set; }

    [Column("template_id")]
    public int? TemplateId { get; set; }

    [Column("recipient_id")]
    public int RecipientId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("recipient_type")]
    public string RecipientType { get; set; } = "client";

    [Required]
    [MaxLength(20)]
    [Column("channel")]
    public string Channel { get; set; } = NotificationChannel.InApp;

    [Required]
    [MaxLength(200)]
    [Column("subject")]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [Column("body")]
    public string Body { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = NotificationStatus.Queued;

    [Column("sent_at")]
    public DateTime? SentAt { get; set; }

    [Column("read_at")]
    public DateTime? ReadAt { get; set; }

    [MaxLength(500)]
    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("OrganizationId")]
    public Organization Organization { get; set; } = null!;

    [ForeignKey("TemplateId")]
    public NotificationTemplate? Template { get; set; }
}

public static class NotificationStatus
{
    public const string Queued = "queued";
    public const string Sent = "sent";
    public const string Failed = "failed";
    public const string Read = "read";
}
