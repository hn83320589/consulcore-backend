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
    public async Task<IActionResult> GetAll()
    {
        var rooms = await _roomService.GetAllAsync();
        return Ok(ApiResponse<List<RoomResponseDto>>.Ok(rooms));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var room = await _roomService.GetByIdAsync(id);
        return Ok(ApiResponse<RoomResponseDto>.Ok(room));
    }

    [HttpGet("active-count")]
    public async Task<IActionResult> GetActiveCount()
    {
        var count = await _roomService.GetActiveCountAsync();
        return Ok(ApiResponse<object>.Ok(new { activeCount = count }));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoomDto dto)
    {
        var orgId = int.Parse(User.FindFirst("organizationId")!.Value);
        var room = await _roomService.CreateAsync(dto, orgId);
        return Ok(ApiResponse<RoomResponseDto>.Ok(room, "診間已建立"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRoomDto dto)
    {
        var room = await _roomService.UpdateAsync(id, dto);
        return Ok(ApiResponse<RoomResponseDto>.Ok(room, "診間已更新"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _roomService.DeleteAsync(id);
        return Ok(ApiResponse<object>.Ok(new { }, "診間已刪除"));
    }

    [HttpPost("{id}/assign-therapist")]
    public async Task<IActionResult> AssignTherapist(int id, [FromBody] AssignTherapistToRoomDto dto)
    {
        var orgId = int.Parse(User.FindFirst("organizationId")!.Value);
        var room = await _roomService.AssignTherapistAsync(id, dto, orgId);
        return Ok(ApiResponse<RoomResponseDto>.Ok(room, "心理師已指派至診間"));
    }

    [HttpDelete("{id}/therapist/{therapistUserId}")]
    public async Task<IActionResult> RemoveTherapist(int id, int therapistUserId)
    {
        var room = await _roomService.RemoveTherapistAsync(id, therapistUserId);
        return Ok(ApiResponse<RoomResponseDto>.Ok(room, "心理師已從診間移除"));
    }
}
