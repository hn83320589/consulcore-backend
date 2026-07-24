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
    [Authorize(Policy = "CanViewConsents")]
    public async Task<IActionResult> GetAll()
    {
        var templates = await _consentService.GetAllTemplatesAsync();
        return Ok(ApiResponse<List<ConsentTemplateResponseDto>>.Ok(templates));
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "CanViewConsents")]
    public async Task<IActionResult> GetById(int id)
    {
        var template = await _consentService.GetTemplateByIdAsync(id);
        return Ok(ApiResponse<ConsentTemplateResponseDto>.Ok(template));
    }

    [HttpPost]
    [Authorize(Policy = "CanCreateConsents")]
    public async Task<IActionResult> Create([FromBody] CreateConsentTemplateDto dto)
    {
        var orgId = int.Parse(User.FindFirst("organizationId")!.Value);
        var template = await _consentService.CreateTemplateAsync(dto, orgId);
        return Ok(ApiResponse<ConsentTemplateResponseDto>.Ok(template, "同意書範本已建立"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanUpdateConsents")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateConsentTemplateDto dto)
    {
        var template = await _consentService.UpdateTemplateAsync(id, dto);
        return Ok(ApiResponse<ConsentTemplateResponseDto>.Ok(template, "同意書範本已更新"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanUpdateConsents")]
    public async Task<IActionResult> Delete(int id)
    {
        await _consentService.DeleteTemplateAsync(id);
        return Ok(ApiResponse<object>.Ok(new { }, "同意書範本已刪除"));
    }
}
