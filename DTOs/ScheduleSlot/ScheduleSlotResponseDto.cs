using consulcore_backend.Models;
using consulcore_backend.OpenApi;

namespace consulcore_backend.DTOs.ScheduleSlot;

public class ScheduleSlotResponseDto
{
    public int Id { get; set; }
    public int TherapistUserId { get; set; }
    public string TherapistName { get; set; } = string.Empty;
    public int? RoomId { get; set; }
    public string? RoomName { get; set; }
    public string Date { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    [ConstantValues(typeof(SlotStatus))]
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class ExpandScheduleDto
{
    public DateOnly StartDate { get; set; } // "2026-04-07"
    public DateOnly EndDate { get; set; }   // "2026-04-30"
}

public class ManualExpandDto
{
    public int Year { get; set; }
    public int Month { get; set; }
}

public class BlockSlotDto
{
    public string? Note { get; set; }
}

public class ExpandResultDto
{
    public int CreatedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> Warnings { get; set; } = [];
}
