namespace consulcore_backend.DTOs.FeeSchedule;

public class UpdateFeeScheduleDto
{
    public string? Name { get; set; }
    public decimal? Amount { get; set; }
    public bool? IsActive { get; set; }
}
