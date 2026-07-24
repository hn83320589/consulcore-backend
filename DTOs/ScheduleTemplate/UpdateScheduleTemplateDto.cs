namespace consulcore_backend.DTOs.ScheduleTemplate;

public class UpdateScheduleTemplateDto
{
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public int? SlotDuration { get; set; }
    public bool? IsActive { get; set; }
}
