namespace consulcore_backend.DTOs.FeeSchedule;

public class FeeScheduleResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SessionType { get; set; } = string.Empty;
    public string VisitType { get; set; } = string.Empty;
    public int? TherapistUserId { get; set; }
    public string? TherapistName { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TWD";
    public bool IsActive { get; set; }
}
