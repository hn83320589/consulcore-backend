using consulcore_backend.Authorization;
using consulcore_backend.DTOs.ConsentTemplate;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/consent-templates")]
[Authorize]
public class ConsentTemplatesController : ControllerBase
{
    private readonly ConsentService _consentService;

    public ConsentTemplatesController(ConsentService consentService)
    {
        _consentService = consentService;
    }

    [HttpGet]
    [Authorize(Policy = Policies.CanViewConsents)]
    public async Task<ActionResult<ApiResponse<List<ConsentTemplateResponseDto>>>> GetAll()
    {
        var templates = await _consentService.GetAllTemplatesAsync();
        return ApiResponse<List<ConsentTemplateResponseDto>>.Ok(templates);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = Policies.CanViewConsents)]
    public async Task<ActionResult<ApiResponse<ConsentTemplateResponseDto>>> GetById(int id)
    {
        var template = await _consentService.GetTemplateByIdAsync(id);
        return ApiResponse<ConsentTemplateResponseDto>.Ok(template);
    }

    [HttpPost]
    [Authorize(Policy = Policies.CanCreateConsents)]
    public async Task<ActionResult<ApiResponse<ConsentTemplateResponseDto>>> Create([FromBody] CreateConsentTemplateDto dto)
    {
        var orgId = User.GetOrganizationId();
        var template = await _consentService.CreateTemplateAsync(dto, orgId);
        return ApiResponse<ConsentTemplateResponseDto>.Ok(template, "同意書範本已建立");
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.CanUpdateConsents)]
    public async Task<ActionResult<ApiResponse<ConsentTemplateResponseDto>>> Update(int id, [FromBody] UpdateConsentTemplateDto dto)
    {
        var template = await _consentService.UpdateTemplateAsync(id, dto);
        return ApiResponse<ConsentTemplateResponseDto>.Ok(template, "同意書範本已更新");
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.CanUpdateConsents)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await _consentService.DeleteTemplateAsync(id);
        return ApiResponse<object>.Ok(new { }, "同意書範本已刪除");
    }
}
