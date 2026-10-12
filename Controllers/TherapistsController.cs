using consulcore_backend.Authorization;
using consulcore_backend.DTOs.User;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace consulcore_backend.Controllers;

[ApiController]
[Route("api/therapists")]
[Authorize(Policy = Policies.CanListTherapists)]
public class TherapistsController : ControllerBase
{
    private readonly UserService _userService;

    public TherapistsController(UserService userService)
    {
        _userService = userService;
    }

    /// <summary>機構內啟用中的心理師（下拉選單用）</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<TherapistOptionDto>>>> GetOptions()
    {
        var therapists = await _userService.GetTherapistOptionsAsync();
        return ApiResponse<List<TherapistOptionDto>>.Ok(therapists);
    }
}
