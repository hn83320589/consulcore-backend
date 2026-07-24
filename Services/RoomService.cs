using consulcore_backend.Data;
using consulcore_backend.DTOs.Room;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace consulcore_backend.Services;

public class RoomService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public RoomService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<List<RoomResponseDto>> GetAllAsync()
    {
        var rooms = await _db.Rooms
            .Include(r => r.TherapistRooms)
                .ThenInclude(tr => tr.TherapistUser)
            .OrderBy(r => r.Name)
            .ToListAsync();

        return rooms.Select(MapToDto).ToList();
    }

    public async Task<RoomResponseDto> GetByIdAsync(int id)
    {
        var room = await _db.Rooms
            .Include(r => r.TherapistRooms)
                .ThenInclude(tr => tr.TherapistUser)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException("診間不存在");

        return MapToDto(room);
    }

    public async Task<int> GetActiveCountAsync()
    {
        return await _db.Rooms.CountAsync(r => r.IsActive);
    }

    public async Task<RoomResponseDto> CreateAsync(CreateRoomDto dto, int orgId)
    {
        var room = new Room
        {
            OrganizationId = orgId,
            Name = dto.Name,
        };
        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("create", "room", room.Id, $"建立診間「{room.Name}」");
        return MapToDto(room);
    }

    public async Task<RoomResponseDto> UpdateAsync(int id, UpdateRoomDto dto)
    {
        var room = await _db.Rooms.FindAsync(id)
            ?? throw new KeyNotFoundException("診間不存在");

        if (dto.Name != null) room.Name = dto.Name;
        if (dto.IsActive.HasValue) room.IsActive = dto.IsActive.Value;
        room.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.LogAsync("update", "room", id, $"更新診間「{room.Name}」");

        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        var room = await _db.Rooms
            .Include(r => r.TherapistRooms)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException("診間不存在");

        _db.TherapistRooms.RemoveRange(room.TherapistRooms);
        _db.Rooms.Remove(room);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("delete", "room", id, $"刪除診間「{room.Name}」");
    }

    public async Task<RoomResponseDto> AssignTherapistAsync(int roomId, AssignTherapistToRoomDto dto, int orgId)
    {
        var room = await _db.Rooms.FindAsync(roomId)
            ?? throw new KeyNotFoundException("診間不存在");

        var exists = await _db.TherapistRooms
            .AnyAsync(tr => tr.RoomId == roomId && tr.TherapistUserId == dto.TherapistUserId);
        if (exists)
            throw new InvalidOperationException("此心理師已指派至該診間");

        _db.TherapistRooms.Add(new TherapistRoom
        {
            OrganizationId = orgId,
            TherapistUserId = dto.TherapistUserId,
            RoomId = roomId,
        });
        await _db.SaveChangesAsync();

        await _audit.LogAsync("assign_therapist", "room", roomId,
            $"指派心理師 (userId={dto.TherapistUserId}) 至診間「{room.Name}」");

        return await GetByIdAsync(roomId);
    }

    public async Task<RoomResponseDto> RemoveTherapistAsync(int roomId, int therapistUserId)
    {
        var tr = await _db.TherapistRooms
            .FirstOrDefaultAsync(t => t.RoomId == roomId && t.TherapistUserId == therapistUserId)
            ?? throw new KeyNotFoundException("找不到此心理師與診間的指派關係");

        _db.TherapistRooms.Remove(tr);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("remove_therapist", "room", roomId,
            $"移除心理師 (userId={therapistUserId}) 從診間");

        return await GetByIdAsync(roomId);
    }

    private static RoomResponseDto MapToDto(Room room)
    {
        return new RoomResponseDto
        {
            Id = room.Id,
            Name = room.Name,
            IsActive = room.IsActive,
            Therapists = room.TherapistRooms?.Select(tr => new TherapistRoomInfoDto
            {
                TherapistUserId = tr.TherapistUserId,
                TherapistName = tr.TherapistUser?.DisplayName ?? "",
                AssignedAt = tr.AssignedAt,
            }).ToList() ?? [],
            CreatedAt = room.CreatedAt,
            UpdatedAt = room.UpdatedAt,
        };
    }
}
