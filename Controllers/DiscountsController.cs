using consulcore_backend.Authorization;
using consulcore_backend.DTOs.Discount;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/discounts")]
[Authorize(Policy = Policies.CanViewBilling)]
public class DiscountsController : ControllerBase
{
    private readonly DiscountService _service;
    public DiscountsController(DiscountService service) { _service = service; }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<DiscountResponseDto>>>> GetAll()
    {
        var discounts = await _service.GetAllAsync();
        return ApiResponse<List<DiscountResponseDto>>.Ok(discounts);
    }

    [HttpPost]
    [Authorize(Policy = Policies.CanCreateBilling)]
    public async Task<ActionResult<ApiResponse<DiscountResponseDto>>> Create([FromBody] CreateDiscountDto dto)
    {
        var orgId = User.GetOrganizationId();
        var discount = await _service.CreateAsync(dto, orgId);
        return ApiResponse<DiscountResponseDto>.Ok(discount, "折扣已建立");
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.CanUpdateBilling)]
    public async Task<ActionResult<ApiResponse<DiscountResponseDto>>> Update(int id, [FromBody] UpdateDiscountDto dto)
    {
        var discount = await _service.UpdateAsync(id, dto);
        return ApiResponse<DiscountResponseDto>.Ok(discount, "折扣已更新");
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.CanUpdateBilling)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return ApiResponse<object>.Ok(new { }, "折扣已刪除");
    }

    [HttpPost("assign")]
    [Authorize(Policy = Policies.CanUpdateBilling)]
    public async Task<ActionResult<ApiResponse<object>>> Assign([FromBody] AssignDiscountDto dto)
    {
        var orgId = User.GetOrganizationId();
        var userId = User.GetUserId();
        await _service.AssignDiscountAsync(dto, orgId, userId);
        return ApiResponse<object>.Ok(new { }, "折扣已指派");
    }

    [HttpDelete("{discountId}/client/{clientProfileId}")]
    [Authorize(Policy = Policies.CanUpdateBilling)]
    public async Task<ActionResult<ApiResponse<object>>> Remove(int discountId, int clientProfileId)
    {
        await _service.RemoveDiscountAsync(clientProfileId, discountId);
        return ApiResponse<object>.Ok(new { }, "折扣已移除");
    }
}
