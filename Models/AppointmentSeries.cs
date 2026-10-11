using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace consulcore_backend.Models;

[Table("appointment_series")]
public class AppointmentSeries
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("organization_id")]
    public int OrganizationId { get; set; }

    [Column("client_profile_id")]
    public int ClientProfileId { get; set; }

    [Column("therapist_user_id")]
    public int TherapistUserId { get; set; }

    [Column("day_of_week")]
    public int DayOfWeek { get; set; }

    [Required]
    [Column("start_time")]
    public TimeOnly StartTime { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("frequency")]
    public string Frequency { get; set; } = SeriesFrequency.Weekly;

    [Required]
    [MaxLength(30)]
    [Column("status")]
    public string Status { get; set; } = SeriesStatus.Active;

    [Column("expires_at")]
    public DateOnly ExpiresAt { get; set; }

    // 已寄出續約提醒的時間；續約後清空。每個系列只提醒一次，避免每天 / 每次重啟重複寄送
    [Column("renewal_reminded_at")]
    public DateTime? RenewalRemindedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("OrganizationId")]
    public Organization Organization { get; set; } = null!;

    [ForeignKey("ClientProfileId")]
    public ClientProfile ClientProfile { get; set; } = null!;

    [ForeignKey("TherapistUserId")]
    public User TherapistUser { get; set; } = null!;

    public ICollection<Appointment> Appointments { get; set; } = [];
}

public static class SeriesFrequency
{
    public const string Weekly = "weekly";
    public const string Biweekly = "biweekly";
}

public static class SeriesStatus
{
    public const string Active = "active";
    public const string PendingConfirmation = "pending_confirmation";
    public const string Ended = "ended";
}
