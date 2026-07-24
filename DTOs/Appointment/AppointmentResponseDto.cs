namespace consulcore_backend.DTOs.Appointment;

public class AppointmentResponseDto
{
    public int Id { get; set; }
    public int ClientProfileId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public int TherapistUserId { get; set; }
    public string TherapistName { get; set; } = string.Empty;
    public int? RoomId { get; set; }
    public string? RoomName { get; set; }
    public int ScheduleSlotId { get; set; }
    public string Date { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int? SeriesId { get; set; }
    public string SessionType { get; set; } = string.Empty;
    public string VisitType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public List<string> Hints { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

public class RescheduleDto
{
    public int NewScheduleSlotId { get; set; }
}

public class AppointmentNoteDto
{
    public string? Note { get; set; }
}
