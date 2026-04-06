using IdentityService.Models;
using IdentityService.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController(IUserRepository userRepo) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var users = await userRepo.GetAllAsync(page, pageSize);
        return Ok(users.Select(u => new UserDto(u.Id, u.FullName, u.Email, u.Role, u.IsEmailVerified, u.CreatedAt)));
    }

    [HttpPatch("{userId:guid}/role")]
    public async Task<IActionResult> UpdateRole(Guid userId, [FromBody] UpdateRoleRequest request)
    {
        var user = await userRepo.GetByIdAsync(userId);
        if (user is null) return NotFound();
        user.Role = request.Role;
        await userRepo.UpdateAsync(user);
        return Ok(new UserDto(user.Id, user.FullName, user.Email, user.Role, user.IsEmailVerified, user.CreatedAt));
    }

    [HttpPatch("{userId:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid userId)
    {
        var user = await userRepo.GetByIdAsync(userId);
        if (user is null) return NotFound();
        user.IsActive = false;
        await userRepo.UpdateAsync(user);
        return NoContent();
    }
}

public record UpdateRoleRequest(string Role);
