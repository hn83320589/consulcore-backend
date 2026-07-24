namespace consulcore_backend.DTOs.Package;

public class UpdatePackageDto
{
    public string? Name { get; set; }
    public decimal? TotalAmount { get; set; }
    public int? ValidDays { get; set; }
    public bool? IsActive { get; set; }
}
