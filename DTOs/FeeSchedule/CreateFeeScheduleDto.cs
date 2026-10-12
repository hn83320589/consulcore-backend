using System.ComponentModel.DataAnnotations;
using consulcore_backend.Models;
using consulcore_backend.OpenApi;

namespace consulcore_backend.DTOs.FeeSchedule;

public class CreateFeeScheduleDto
{
    [Required] [MaxLength(100)] public string Name { get; set; } = string.Empty;
    [ConstantValues(typeof(AppointmentSessionType))]
    [MaxLength(20)] public string SessionType { get; set; } = "individual";
    [ConstantValues(typeof(AppointmentVisitType))]
    [MaxLength(20)] public string VisitType { get; set; } = "follow_up";
    public int? TherapistUserId { get; set; }
    [Required] public decimal Amount { get; set; }
}
