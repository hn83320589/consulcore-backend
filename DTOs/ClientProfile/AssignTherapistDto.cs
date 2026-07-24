using System.ComponentModel.DataAnnotations;

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
    public string Status { get; set; } = string.Empty;
}
