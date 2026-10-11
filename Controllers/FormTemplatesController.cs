using consulcore_backend.Authorization;
using consulcore_backend.DTOs.FormTemplate;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/form-templates")]
// 範本含初診評估的高敏感題目：讀取限機構人員，修改限有機構設定權限者
[Authorize(Policy = Policies.CanViewClients)]
public class FormTemplatesController : ControllerBase
{
    private readonly FormService _formService;

    public FormTemplatesController(FormService formService)
    {
        _formService = formService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var templates = await _formService.GetAllTemplatesAsync();
        return Ok(ApiResponse<List<FormTemplateResponseDto>>.Ok(templates));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var template = await _formService.GetTemplateByIdAsync(id);
        return Ok(ApiResponse<FormTemplateResponseDto>.Ok(template));
    }

    [HttpPost]
    [Authorize(Policy = Policies.CanManageOrgSettings)]
    public async Task<IActionResult> Create([FromBody] CreateFormTemplateDto dto)
    {
        var orgId = User.GetOrganizationId();
        var userId = User.GetUserId();
        var template = await _formService.CreateTemplateAsync(dto, orgId, userId);
        return Ok(ApiResponse<FormTemplateResponseDto>.Ok(template, "表單範本已建立"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.CanManageOrgSettings)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFormTemplateDto dto)
    {
        var template = await _formService.UpdateTemplateAsync(id, dto);
        return Ok(ApiResponse<FormTemplateResponseDto>.Ok(template, "表單範本已更新"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.CanManageOrgSettings)]
    public async Task<IActionResult> Delete(int id)
    {
        await _formService.DeleteTemplateAsync(id);
        return Ok(ApiResponse<object>.Ok(new { }, "表單範本已刪除"));
    }

    [HttpPost("{id}/clone")]
    [Authorize(Policy = Policies.CanManageOrgSettings)]
    public async Task<IActionResult> Clone(int id)
    {
        var orgId = User.GetOrganizationId();
        var userId = User.GetUserId();
        var template = await _formService.CloneTemplateAsync(id, orgId, userId);
        return Ok(ApiResponse<FormTemplateResponseDto>.Ok(template, "表單範本已複製"));
    }
}
