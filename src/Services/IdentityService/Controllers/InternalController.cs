using IdentityService.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

/// <summary>
/// Internal service-to-service endpoints — not exposed through the API Gateway.
/// </summary>
[ApiController]
[Route("internal")]
public class InternalController(IUserRepository userRepo) : ControllerBase
{
    /// <summary>Returns minimal user info needed for email delivery by other services.</summary>
    [HttpGet("users/{userId:guid}")]
    public async Task<IActionResult> GetUserInfo(Guid userId)
    {
        var user = await userRepo.GetByIdAsync(userId);
        if (user is null) return NotFound();
        return Ok(new { user.Id, user.FullName, user.Email });
    }
}
