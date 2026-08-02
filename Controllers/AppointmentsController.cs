using consulcore_backend.DTOs.Appointment;
using consulcore_backend.DTOs.ScheduleSlot;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly AppointmentService _appointmentService;
    private readonly ClientService _clientService;

    public AppointmentsController(AppointmentService appointmentService, ClientService clientService)
    {
        _appointmentService = appointmentService;
        _clientService = clientService;
    }

    private bool IsClientRole() => Request.Headers["X-Active-Role"].FirstOrDefault() == "client";
    private int CurrentUserId() => int.Parse(User.FindFirst("userId")!.Value);

    // client 角色只能存取自己的個案檔 id；取不到（帳號無對應個案）回 null
    private Task<int?> CurrentClientProfileIdAsync() => _clientService.GetClientProfileIdByUserAsync(CurrentUserId());

    [HttpPost]
    [Authorize(Policy = "CanCreateAppointments")]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentDto dto)
    {
        var orgId = int.Parse(User.FindFirst("organizationId")!.Value);
        var userId = int.Parse(User.FindFirst("userId")!.Value);
        var appointment = await _appointmentService.CreateAsync(dto, orgId, userId);
        return Ok(ApiResponse<AppointmentResponseDto>.Ok(appointment, "預約已建立"));
    }

    [HttpPost("{id}/complete")]
    [Authorize(Policy = "CanUpdateAppointments")]
    public async Task<IActionResult> Complete(int id)
    {
        var appointment = await _appointmentService.CompleteAsync(id);
        return Ok(ApiResponse<AppointmentResponseDto>.Ok(appointment, "預約已完成"));
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Policy = "CanUpdateAppointments")]
    public async Task<IActionResult> Cancel(int id, [FromBody] AppointmentNoteDto? dto)
    {
        var appointment = await _appointmentService.CancelAsync(id, dto);
        return Ok(ApiResponse<AppointmentResponseDto>.Ok(appointment, "預約已取消"));
    }

    [HttpPost("{id}/no-show")]
    [Authorize(Policy = "CanUpdateAppointments")]
    public async Task<IActionResult> MarkNoShow(int id)
    {
        var appointment = await _appointmentService.MarkNoShowAsync(id);
        return Ok(ApiResponse<AppointmentResponseDto>.Ok(appointment, "已標記未到"));
    }

    [HttpPost("{id}/reschedule")]
    [Authorize(Policy = "CanUpdateAppointments")]
    public async Task<IActionResult> Reschedule(int id, [FromBody] RescheduleDto dto)
    {
        var appointment = await _appointmentService.RescheduleAsync(id, dto);
        return Ok(ApiResponse<AppointmentResponseDto>.Ok(appointment, "預約已改期"));
    }

    [HttpGet("calendar")]
    [Authorize(Policy = "CanViewAppointments")]
    public async Task<IActionResult> CalendarView(
        [FromQuery] string startDate, [FromQuery] string endDate, [FromQuery] int? therapistUserId)
    {
        // 行事曆為 staff 聚合視圖，無法依個案範圍化；個案請改用 list 查自己的預約
        if (IsClientRole())
            return StatusCode(403, ApiResponse<object>.Fail("個案無法存取行事曆檢視"));

        var start = DateOnly.Parse(startDate);
        var end = DateOnly.Parse(endDate);
        var appointments = await _appointmentService.GetByDateRangeAsync(start, end, therapistUserId);
        return Ok(ApiResponse<List<AppointmentResponseDto>>.Ok(appointments));
    }

    [HttpGet("list")]
    [Authorize(Policy = "CanViewAppointments")]
    public async Task<IActionResult> ListView(
        [FromQuery] int? clientProfileId, [FromQuery] int? therapistUserId, [FromQuery] string? status)
    {
        // client 角色一律限縮為自己的個案，忽略傳入的 clientProfileId（防越權查他人）
        if (IsClientRole())
        {
            var own = await CurrentClientProfileIdAsync();
            if (own == null) return Ok(ApiResponse<List<AppointmentResponseDto>>.Ok([]));
            clientProfileId = own;
            therapistUserId = null;
        }
        var appointments = await _appointmentService.GetListAsync(clientProfileId, therapistUserId, status);
        return Ok(ApiResponse<List<AppointmentResponseDto>>.Ok(appointments));
    }

    [HttpGet("available-slots")]
    [Authorize(Policy = "CanViewAppointments")]
    public async Task<IActionResult> AvailableSlots(
        [FromQuery] string startDate, [FromQuery] string endDate, [FromQuery] int? therapistUserId)
    {
        var start = DateOnly.Parse(startDate);
        var end = DateOnly.Parse(endDate);
        var slots = await _appointmentService.GetAvailableSlotsAsync(therapistUserId, start, end);
        return Ok(ApiResponse<List<ScheduleSlotResponseDto>>.Ok(slots));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "CanViewAppointments")]
    public async Task<IActionResult> GetById(int id)
    {
        var appointment = await _appointmentService.GetByIdAsync(id);

        // client 只能看自己的預約，防止以任意 id 讀取他人（IDOR）
        if (IsClientRole() && appointment.ClientProfileId != await CurrentClientProfileIdAsync())
            return StatusCode(403, ApiResponse<object>.Fail("無權存取此預約"));

        return Ok(ApiResponse<AppointmentResponseDto>.Ok(appointment));
    }
}
