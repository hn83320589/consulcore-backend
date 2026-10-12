using consulcore_backend.Authorization;
using consulcore_backend.DTOs.Organization;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/organization")]
[Authorize(Policy = Policies.CanManageOrgSettings)]
public class OrganizationController : ControllerBase
{
    private readonly OrganizationService _organizationService;

    public OrganizationController(OrganizationService organizationService)
    {
        _organizationService = organizationService;
    }

    /// <summary>目前登入機構的基本資料</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<OrganizationResponseDto>>> Get()
    {
        var org = await _organizationService.GetAsync(User.GetOrganizationId());
        return ApiResponse<OrganizationResponseDto>.Ok(org);
    }

    /// <summary>更新機構基本資料（名稱、電話、Email、地址）</summary>
    [HttpPut]
    public async Task<ActionResult<ApiResponse<OrganizationResponseDto>>> Update([FromBody] UpdateOrganizationDto dto)
    {
        var org = await _organizationService.UpdateAsync(User.GetOrganizationId(), dto);
        return ApiResponse<OrganizationResponseDto>.Ok(org, "機構資料已更新");
    }
}
