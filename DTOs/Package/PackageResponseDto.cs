using consulcore_backend.Models;
using consulcore_backend.OpenApi;

namespace consulcore_backend.DTOs.Package;

public class PackageResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    [ConstantValues(typeof(AppointmentSessionType))]
    public string SessionType { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PerSessionAmount { get; set; }
    public int ValidDays { get; set; }
    public bool IsActive { get; set; }
}
