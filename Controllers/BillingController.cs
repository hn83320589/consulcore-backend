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
    public async Task<ActionResult<ApiResponse<PaymentRecordResponseDto?>>> OnCompleted(int appointmentId)
    {
        var userId = User.GetUserId();
        var record = await _billingService.OnAppointmentCompletedAsync(appointmentId, userId);
        return ApiResponse<PaymentRecordResponseDto?>.Ok(record,
            record == null ? "無適用費率，未產生收費記錄" : "收費記錄已產生");
    }

    [HttpPost("purchase-package")]
    [Authorize(Policy = Policies.CanCreateBilling)]
    public async Task<ActionResult<ApiResponse<ClientPackageResponseDto>>> PurchasePackage([FromBody] PurchasePackageDto dto)
    {
        var orgId = User.GetOrganizationId();
        var userId = User.GetUserId();
        var cp = await _billingService.PurchasePackageAsync(dto, orgId, userId);
        return ApiResponse<ClientPackageResponseDto>.Ok(cp, "套餐已購買");
    }

    [HttpPost("{id}/confirm")]
    [Authorize(Policy = Policies.CanUpdateBilling)]
    public async Task<ActionResult<ApiResponse<PaymentRecordResponseDto>>> Confirm(int id, [FromBody] ConfirmPaymentDto dto)
    {
        var record = await _billingService.ConfirmPaymentAsync(id, dto);
        return ApiResponse<PaymentRecordResponseDto>.Ok(record, "已確認收款");
    }

    [HttpPost("{id}/refund")]
    [Authorize(Policy = Policies.CanRefundBilling)]
    public async Task<ActionResult<ApiResponse<PaymentRecordResponseDto>>> Refund(int id, [FromBody] RefundDto dto)
    {
        var record = await _billingService.RefundAsync(id, dto);
        return ApiResponse<PaymentRecordResponseDto>.Ok(record, "已退費");
    }

    [HttpPost("{id}/waive")]
    [Authorize(Policy = Policies.CanUpdateBilling)]
    public async Task<ActionResult<ApiResponse<PaymentRecordResponseDto>>> Waive(int id)
    {
        var record = await _billingService.WaiveAsync(id);
        return ApiResponse<PaymentRecordResponseDto>.Ok(record, "已免除收費");
    }

    [HttpGet("pending")]
    public async Task<ActionResult<ApiResponse<List<PaymentRecordResponseDto>>>> Pending()
    {
        var records = await _billingService.GetPendingAsync();
        return ApiResponse<List<PaymentRecordResponseDto>>.Ok(records);
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PaymentRecordResponseDto>>>> GetAll(
        [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate,
        [FromQuery] int? clientProfileId, [FromQuery] int? therapistUserId,
        [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var records = await _billingService.GetPaymentsAsync(
            startDate, endDate, clientProfileId, therapistUserId, status, page, pageSize);
        return ApiResponse<List<PaymentRecordResponseDto>>.Ok(records);
    }

    [HttpGet("packages")]
    public async Task<ActionResult<ApiResponse<List<ClientPackageResponseDto>>>> Packages(
        [FromQuery] int? clientProfileId, [FromQuery] string? status)
    {
        var packages = await _billingService.GetClientPackagesAsync(clientProfileId, status);
        return ApiResponse<List<ClientPackageResponseDto>>.Ok(packages);
    }

    [HttpGet("export")]
    [ProducesResponseType<FileResult>(StatusCodes.Status200OK, "text/csv")]
    public async Task<IActionResult> Export(
        [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate,
        [FromQuery] int? clientProfileId, [FromQuery] int? therapistUserId, [FromQuery] string? status)
    {
        var csv = await _billingService.ExportPaymentsCsvAsync(
            startDate, endDate, clientProfileId, therapistUserId, status);

        return File(CsvWriter.ToUtf8WithBom(csv), "text/csv; charset=utf-8", "payments.csv");
    }
}
