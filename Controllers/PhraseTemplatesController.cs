using consulcore_backend.Authorization;
using consulcore_backend.DTOs.PhraseTemplate;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/phrase-templates")]
[Authorize]
public class PhraseTemplatesController : ControllerBase
{
    private readonly PhraseService _phraseService;

    public PhraseTemplatesController(PhraseService phraseService)
    {
        _phraseService = phraseService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = User.GetUserId();
        var phrases = await _phraseService.GetAllAsync(userId);
        return Ok(ApiResponse<List<PhraseTemplateResponseDto>>.Ok(phrases));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePhraseTemplateDto dto)
    {
        var orgId = User.GetOrganizationId();
        var userId = User.GetUserId();
        var phrase = await _phraseService.CreateAsync(dto, orgId, userId);
        return Ok(ApiResponse<PhraseTemplateResponseDto>.Ok(phrase, "常用語範本已建立"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePhraseTemplateDto dto)
    {
        var userId = User.GetUserId();
        var phrase = await _phraseService.UpdateAsync(id, dto, userId);
        return Ok(ApiResponse<PhraseTemplateResponseDto>.Ok(phrase, "常用語範本已更新"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.GetUserId();
        await _phraseService.DeleteAsync(id, userId);
        return Ok(ApiResponse<object>.Ok(new { }, "常用語範本已刪除"));
    }
}
