using System.ComponentModel.DataAnnotations;
using consulcore_backend.Models;
using consulcore_backend.OpenApi;

namespace consulcore_backend.DTOs.Appointment;

public class CreateAppointmentDto
{
    [Required]
    public int ClientProfileId { get; set; }

    [Required]
    public int ScheduleSlotId { get; set; }

    [MaxLength(20)]
    [ConstantValues(typeof(AppointmentSessionType))]
    public string SessionType { get; set; } = "individual";

    [MaxLength(20)]
    [ConstantValues(typeof(AppointmentVisitType))]
    public string VisitType { get; set; } = "follow_up";

    [MaxLength(500)]
    public string? Note { get; set; }
}
