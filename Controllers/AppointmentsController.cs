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

    public AppointmentsController(AppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

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
}
