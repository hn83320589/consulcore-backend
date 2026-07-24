using System.ComponentModel.DataAnnotations;

namespace consulcore_backend.DTOs.FeeSchedule;

public class CreateFeeScheduleDto
{
    [Required] [MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(20)] public string SessionType { get; set; } = "individual";
    [MaxLength(20)] public string VisitType { get; set; } = "follow_up";
    public int? TherapistUserId { get; set; }
    [Required] public decimal Amount { get; set; }
}
