using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace consulcore_backend.Models;

[Table("notification_preferences")]
public class NotificationPreference
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("event_type")]
    public string EventType { get; set; } = string.Empty;

    [Column("email_enabled")]
    public bool EmailEnabled { get; set; } = true;

    [Column("sms_enabled")]
    public bool SmsEnabled { get; set; } = true;

    [Column("in_app_enabled")]
    public bool InAppEnabled { get; set; } = true;

    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
}
