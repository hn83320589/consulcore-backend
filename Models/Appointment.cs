using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace consulcore_backend.Models;

[Table("appointments")]
public class Appointment
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

    [Column("room_id")]
    public int? RoomId { get; set; }

    [Column("schedule_slot_id")]
    public int ScheduleSlotId { get; set; }

    [Column("series_id")]
    public int? SeriesId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("session_type")]
    public string SessionType { get; set; } = AppointmentSessionType.Individual;

    [Required]
    [MaxLength(20)]
    [Column("visit_type")]
    public string VisitType { get; set; } = AppointmentVisitType.FollowUp;

    [Required]
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = AppointmentStatus.Scheduled;

    [MaxLength(500)]
    [Column("note")]
    public string? Note { get; set; }

    [Column("created_by")]
    public int CreatedBy { get; set; }

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

    [ForeignKey("RoomId")]
    public Room? Room { get; set; }

    [ForeignKey("ScheduleSlotId")]
    public ScheduleSlot ScheduleSlot { get; set; } = null!;

    [ForeignKey("SeriesId")]
    public AppointmentSeries? Series { get; set; }
}

public static class AppointmentSessionType
{
    public const string Individual = "individual";
    public const string Couple = "couple";
    public const string Family = "family";
}

public static class AppointmentVisitType
{
    public const string Initial = "initial";
    public const string FollowUp = "follow_up";
}

public static class AppointmentStatus
{
    public const string Scheduled = "scheduled";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";
    public const string NoShow = "no_show";
}
