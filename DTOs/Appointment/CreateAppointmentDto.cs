using System.ComponentModel.DataAnnotations;

namespace consulcore_backend.DTOs.Appointment;

public class CreateAppointmentDto
{
    [Required]
    public int ClientProfileId { get; set; }

    [Required]
    public int ScheduleSlotId { get; set; }

    [MaxLength(20)]
    public string SessionType { get; set; } = "individual";

    [MaxLength(20)]
    public string VisitType { get; set; } = "follow_up";

    [MaxLength(500)]
    public string? Note { get; set; }
}
