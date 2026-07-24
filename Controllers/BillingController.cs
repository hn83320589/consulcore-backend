using consulcore_backend.DTOs.Billing;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/billing")]
[Authorize(Policy = "CanViewBilling")]
public class BillingController : ControllerBase
{
    private readonly BillingService _billingService;
    public BillingController(BillingService billingService) { _billingService = billingService; }

    [HttpPost("on-completed/{appointmentId}")]
    [Authorize(Policy = "CanCreateBilling")]
    public async Task<IActionResult> OnCompleted(int appointmentId)
    {
        var userId = int.Parse(User.FindFirst("userId")!.Value);
        var record = await _billingService.OnAppointmentCompletedAsync(appointmentId, userId);
        if (record == null)
            return Ok(ApiResponse<object>.Ok(new { }, "無適用費率，未產生收費記錄"));
        return Ok(ApiResponse<PaymentRecordResponseDto>.Ok(record, "收費記錄已產生"));
    }

    [HttpPost("purchase-package")]
    [Authorize(Policy = "CanCreateBilling")]
    public async Task<IActionResult> PurchasePackage([FromBody] PurchasePackageDto dto)
    {
        var orgId = int.Parse(User.FindFirst("organizationId")!.Value);
        var userId = int.Parse(User.FindFirst("userId")!.Value);
        var cp = await _billingService.PurchasePackageAsync(dto, orgId, userId);
        return Ok(ApiResponse<ClientPackageResponseDto>.Ok(cp, "套餐已購買"));
    }

    [HttpPost("{id}/confirm")]
    [Authorize(Policy = "CanUpdateBilling")]
    public async Task<IActionResult> Confirm(int id, [FromBody] ConfirmPaymentDto dto)
    {
        var record = await _billingService.ConfirmPaymentAsync(id, dto);
        return Ok(ApiResponse<PaymentRecordResponseDto>.Ok(record, "已確認收款"));
    }

    [HttpPost("{id}/refund")]
    [Authorize(Policy = "CanRefundBilling")]
    public async Task<IActionResult> Refund(int id, [FromBody] RefundDto dto)
    {
        var record = await _billingService.RefundAsync(id, dto);
        return Ok(ApiResponse<PaymentRecordResponseDto>.Ok(record, "已退費"));
    }

    [HttpPost("{id}/waive")]
    [Authorize(Policy = "CanUpdateBilling")]
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
}
