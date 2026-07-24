using consulcore_backend.DTOs.FormSubmission;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/form-submissions")]
[Authorize]
public class FormSubmissionsController : ControllerBase
{
    private readonly FormService _formService;

    public FormSubmissionsController(FormService formService)
    {
        _formService = formService;
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitFormDto dto)
    {
        var orgId = int.Parse(User.FindFirst("organizationId")!.Value);
        var userId = int.Parse(User.FindFirst("userId")!.Value);
        var submission = await _formService.SubmitFormAsync(dto, orgId, userId);
        return Ok(ApiResponse<FormSubmissionResponseDto>.Ok(submission, "表單已提交"));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var submission = await _formService.GetSubmissionByIdAsync(id);
        return Ok(ApiResponse<FormSubmissionResponseDto>.Ok(submission));
    }

    [HttpGet("by-client/{clientProfileId}")]
    public async Task<IActionResult> GetByClient(int clientProfileId)
    {
        var submissions = await _formService.GetSubmissionsByClientAsync(clientProfileId);
        return Ok(ApiResponse<List<FormSubmissionResponseDto>>.Ok(submissions));
    }
}
