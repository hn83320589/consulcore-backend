using System.ComponentModel.DataAnnotations;
using consulcore_backend.Models;
using consulcore_backend.OpenApi;

namespace consulcore_backend.DTOs.ClientProfile;

public class AssignTherapistDto
{
    [Required]
    public int TherapistUserId { get; set; }
}

public class TransferTherapistDto
{
    [Required]
    public int NewTherapistUserId { get; set; }

    public string? Reason { get; set; }
}

public class ChangeStatusDto
{
    [Required]
    [ConstantValues(typeof(ClientStatus))]
    public string Status { get; set; } = string.Empty;
}
