using System.ComponentModel.DataAnnotations;

namespace consulcore_backend.DTOs.ScheduleTemplate;

public class CreateScheduleTemplateDto
{
    [Required]
    public int TherapistUserId { get; set; }

    [Required]
    [Range(0, 6)]
    public int DayOfWeek { get; set; }

    [Required]
    public string StartTime { get; set; } = string.Empty; // "09:00"

    [Required]
    public string EndTime { get; set; } = string.Empty; // "17:00"

    /// <summary>每個時段的長度（分鐘，10–120）</summary>
    [Range(10, 120)]
    public int SlotDuration { get; set; } = 50;
}
