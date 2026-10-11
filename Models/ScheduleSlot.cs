using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace consulcore_backend.Models;

[Table("schedule_slots")]
public class ScheduleSlot
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("organization_id")]
    public int OrganizationId { get; set; }

    [Column("therapist_user_id")]
    public int TherapistUserId { get; set; }

    [Column("room_id")]
    public int? RoomId { get; set; }

    [Required]
    [Column("date")]
    public DateOnly Date { get; set; }

    [Required]
    [Column("start_time")]
    public TimeOnly StartTime { get; set; }

    [Required]
    [Column("end_time")]
    public TimeOnly EndTime { get; set; }

    [Required]
    [MaxLength(20)]
    // 樂觀併發：UPDATE 會帶 WHERE status = 讀取時的值，兩人同時預約同一時段時後寫者失敗（→ 409）
    [Column("status")]
    [ConcurrencyCheck]
    public string Status { get; set; } = SlotStatus.Available;

    [Column("is_reserved_hold")]
    public bool IsReservedHold { get; set; }

    [MaxLength(500)]
    [Column("note")]
    public string? Note { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("OrganizationId")]
    public Organization Organization { get; set; } = null!;

    [ForeignKey("TherapistUserId")]
    public User TherapistUser { get; set; } = null!;

    [ForeignKey("RoomId")]
    public Room? Room { get; set; }
}

public static class SlotStatus
{
    public const string Available = "available";
    public const string Reserved = "reserved";
    public const string Blocked = "blocked";
    public const string Cancelled = "cancelled";
}
