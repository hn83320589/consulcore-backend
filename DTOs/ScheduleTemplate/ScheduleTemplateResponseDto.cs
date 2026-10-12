namespace consulcore_backend.DTOs.ScheduleTemplate;

public class ScheduleTemplateResponseDto
{
    public int Id { get; set; }
    public int TherapistUserId { get; set; }
    public string TherapistName { get; set; } = string.Empty;
    public int DayOfWeek { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    /// <summary>每個時段的長度（分鐘）</summary>
    public int SlotDuration { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
