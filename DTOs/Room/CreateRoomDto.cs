using System.ComponentModel.DataAnnotations;

namespace consulcore_backend.DTOs.Room;

public class CreateRoomDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
