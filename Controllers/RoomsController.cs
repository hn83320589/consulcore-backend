using consulcore_backend.Authorization;
using consulcore_backend.DTOs.Room;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/rooms")]
[Authorize(Policy = Policies.CanManageRooms)]
public class RoomsController : ControllerBase
{
    private readonly RoomService _roomService;

    public RoomsController(RoomService roomService)
    {
        _roomService = roomService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RoomResponseDto>>>> GetAll()
    {
        var rooms = await _roomService.GetAllAsync();
        return ApiResponse<List<RoomResponseDto>>.Ok(rooms);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<RoomResponseDto>>> GetById(int id)
    {
        var room = await _roomService.GetByIdAsync(id);
        return ApiResponse<RoomResponseDto>.Ok(room);
    }

    [HttpGet("active-count")]
    public async Task<ActionResult<ApiResponse<RoomActiveCountDto>>> GetActiveCount()
    {
        var count = await _roomService.GetActiveCountAsync();
        return ApiResponse<RoomActiveCountDto>.Ok(new RoomActiveCountDto { ActiveCount = count });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoomResponseDto>>> Create([FromBody] CreateRoomDto dto)
    {
        var orgId = User.GetOrganizationId();
        var room = await _roomService.CreateAsync(dto, orgId);
        return ApiResponse<RoomResponseDto>.Ok(room, "診間已建立");
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<RoomResponseDto>>> Update(int id, [FromBody] UpdateRoomDto dto)
    {
        var room = await _roomService.UpdateAsync(id, dto);
        return ApiResponse<RoomResponseDto>.Ok(room, "診間已更新");
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await _roomService.DeleteAsync(id);
        return ApiResponse<object>.Ok(new { }, "診間已刪除");
    }

    [HttpPost("{id}/assign-therapist")]
    public async Task<ActionResult<ApiResponse<RoomResponseDto>>> AssignTherapist(int id, [FromBody] AssignTherapistToRoomDto dto)
    {
        var orgId = User.GetOrganizationId();
        var room = await _roomService.AssignTherapistAsync(id, dto, orgId);
        return ApiResponse<RoomResponseDto>.Ok(room, "心理師已指派至診間");
    }

    [HttpDelete("{id}/therapist/{therapistUserId}")]
    public async Task<ActionResult<ApiResponse<RoomResponseDto>>> RemoveTherapist(int id, int therapistUserId)
    {
        var room = await _roomService.RemoveTherapistAsync(id, therapistUserId);
        return ApiResponse<RoomResponseDto>.Ok(room, "心理師已從診間移除");
    }
}
