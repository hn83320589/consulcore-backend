namespace consulcore_backend.DTOs.Room;

public class RoomResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<TherapistRoomInfoDto> Therapists { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TherapistRoomInfoDto
{
    public int TherapistUserId { get; set; }
    public string TherapistName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
}

public class AssignTherapistToRoomDto
{
    public int TherapistUserId { get; set; }
}
