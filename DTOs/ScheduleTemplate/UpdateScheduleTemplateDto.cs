namespace consulcore_backend.DTOs.ScheduleTemplate;

public class UpdateScheduleTemplateDto
{
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    /// <summary>每個時段的長度（分鐘，10–120）</summary>
    public int? SlotDuration { get; set; }
    public bool? IsActive { get; set; }
}
