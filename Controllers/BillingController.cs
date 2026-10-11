using consulcore_backend.Authorization;
using consulcore_backend.DTOs.Billing;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/billing")]
[Authorize(Policy = Policies.CanViewBilling)]
public class BillingController : ControllerBase
{
    private readonly BillingService _billingService;
    public BillingController(BillingService billingService) { _billingService = billingService; }

    [HttpPost("on-completed/{appointmentId}")]
    [Authorize(Policy = Policies.CanCreateBilling)]
    public async Task<IActionResult> OnCompleted(int appointmentId)
    {
        var userId = User.GetUserId();
        var record = await _billingService.OnAppointmentCompletedAsync(appointmentId, userId);
        if (record == null)
            return Ok(ApiResponse<object>.Ok(new { }, "無適用費率，未產生收費記錄"));
        return Ok(ApiResponse<PaymentRecordResponseDto>.Ok(record, "收費記錄已產生"));
    }

    [HttpPost("purchase-package")]
    [Authorize(Policy = Policies.CanCreateBilling)]
    public async Task<IActionResult> PurchasePackage([FromBody] PurchasePackageDto dto)
    {
        var orgId = User.GetOrganizationId();
        var userId = User.GetUserId();
        var cp = await _billingService.PurchasePackageAsync(dto, orgId, userId);
        return Ok(ApiResponse<ClientPackageResponseDto>.Ok(cp, "套餐已購買"));
    }

    [HttpPost("{id}/confirm")]
    [Authorize(Policy = Policies.CanUpdateBilling)]
    public async Task<IActionResult> Confirm(int id, [FromBody] ConfirmPaymentDto dto)
    {
        var record = await _billingService.ConfirmPaymentAsync(id, dto);
        return Ok(ApiResponse<PaymentRecordResponseDto>.Ok(record, "已確認收款"));
    }

    [HttpPost("{id}/refund")]
    [Authorize(Policy = Policies.CanRefundBilling)]
    public async Task<IActionResult> Refund(int id, [FromBody] RefundDto dto)
    {
        var record = await _billingService.RefundAsync(id, dto);
        return Ok(ApiResponse<PaymentRecordResponseDto>.Ok(record, "已退費"));
    }

    [HttpPost("{id}/waive")]
    [Authorize(Policy = Policies.CanUpdateBilling)]
    public async Task<IActionResult> Waive(int id)
    {
        var record = await _billingService.WaiveAsync(id);
        return Ok(ApiResponse<PaymentRecordResponseDto>.Ok(record, "已免除收費"));
    }

    [HttpGet("pending")]
    public async Task<IActionResult> Pending()
    {
        var records = await _billingService.GetPendingAsync();
        return Ok(ApiResponse<List<PaymentRecordResponseDto>>.Ok(records));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate,
        [FromQuery] int? clientProfileId, [FromQuery] int? therapistUserId,
        [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var records = await _billingService.GetPaymentsAsync(
            startDate, endDate, clientProfileId, therapistUserId, status, page, pageSize);
        return Ok(ApiResponse<List<PaymentRecordResponseDto>>.Ok(records));
    }

    [HttpGet("packages")]
    public async Task<IActionResult> Packages(
        [FromQuery] int? clientProfileId, [FromQuery] string? status)
    {
        var packages = await _billingService.GetClientPackagesAsync(clientProfileId, status);
        return Ok(ApiResponse<List<ClientPackageResponseDto>>.Ok(packages));
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate,
        [FromQuery] int? clientProfileId, [FromQuery] int? therapistUserId, [FromQuery] string? status)
    {
        var csv = await _billingService.ExportPaymentsCsvAsync(
            startDate, endDate, clientProfileId, therapistUserId, status);

        var preamble = System.Text.Encoding.UTF8.GetPreamble();
        var body = System.Text.Encoding.UTF8.GetBytes(csv);
        var bytes = new byte[preamble.Length + body.Length];
        Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
        Buffer.BlockCopy(body, 0, bytes, preamble.Length, body.Length);
        return File(bytes, "text/csv; charset=utf-8", "payments.csv");
    }
}
