using consulcore_backend.DTOs.Package;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/packages")]
[Authorize(Policy = "CanViewBilling")]
public class PackagesController : ControllerBase
{
    private readonly PackageService _service;
    public PackagesController(PackageService service) { _service = service; }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var packages = await _service.GetAllAsync();
        return Ok(ApiResponse<List<PackageResponseDto>>.Ok(packages));
    }

    [HttpPost]
    [Authorize(Policy = "CanCreateBilling")]
    public async Task<IActionResult> Create([FromBody] CreatePackageDto dto)
    {
        var orgId = int.Parse(User.FindFirst("organizationId")!.Value);
        var pkg = await _service.CreateAsync(dto, orgId);
        return Ok(ApiResponse<PackageResponseDto>.Ok(pkg, "套餐已建立"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanUpdateBilling")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePackageDto dto)
    {
        var pkg = await _service.UpdateAsync(id, dto);
        return Ok(ApiResponse<PackageResponseDto>.Ok(pkg, "套餐已更新"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanUpdateBilling")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return Ok(ApiResponse<object>.Ok(new { }, "套餐已刪除"));
    }
}
