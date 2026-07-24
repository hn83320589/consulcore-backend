using System.ComponentModel.DataAnnotations;

namespace consulcore_backend.DTOs.Room;

public class UpdateRoomDto
{
    [MaxLength(100)]
    public string? Name { get; set; }

    public bool? IsActive { get; set; }
}
