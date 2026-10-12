using System.ComponentModel.DataAnnotations;

namespace consulcore_backend.DTOs.Organization;

/// <summary>整筆取代：未提供的電話、Email、地址會被清空</summary>
public class UpdateOrganizationDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [EmailAddress]
    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }
}
