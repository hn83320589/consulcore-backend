using consulcore_backend.Authorization;
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
    [Authorize(Policy = Policies.CanUpdateClients)]
    public async Task<ActionResult<ApiResponse<FormSubmissionResponseDto>>> Submit([FromBody] SubmitFormDto dto)
    {
        var orgId = User.GetOrganizationId();
        var userId = User.GetUserId();
        var submission = await _formService.SubmitFormAsync(dto, orgId, userId);
        return ApiResponse<FormSubmissionResponseDto>.Ok(submission, "表單已提交");
    }

    [HttpGet("{id}")]
    [Authorize(Policy = Policies.CanViewClients)]
    public async Task<ActionResult<ApiResponse<FormSubmissionResponseDto>>> GetById(int id)
    {
        var submission = await _formService.GetSubmissionByIdAsync(id);
        return ApiResponse<FormSubmissionResponseDto>.Ok(submission);
    }

    [HttpGet("by-client/{clientProfileId}")]
    [Authorize(Policy = Policies.CanViewClients)]
    public async Task<ActionResult<ApiResponse<List<FormSubmissionResponseDto>>>> GetByClient(int clientProfileId)
    {
        var submissions = await _formService.GetSubmissionsByClientAsync(clientProfileId);
        return ApiResponse<List<FormSubmissionResponseDto>>.Ok(submissions);
    }
}
