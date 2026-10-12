using System.ComponentModel.DataAnnotations;
using consulcore_backend.Models;
using consulcore_backend.OpenApi;

namespace consulcore_backend.DTOs.Package;

public class CreatePackageDto
{
    [Required] [MaxLength(100)] public string Name { get; set; } = string.Empty;
    [ConstantValues(typeof(AppointmentSessionType))]
    [MaxLength(20)] public string SessionType { get; set; } = "individual";
    [Required] public int TotalSessions { get; set; }
    [Required] public decimal TotalAmount { get; set; }
    public int ValidDays { get; set; } = 90;
}
