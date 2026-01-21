using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("admin/users")]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // GET /admin/users
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .OrderBy(u => u.Email)
            .Select(u => new
            {
                userId = u.Id,
                email = u.Email
            })
            .ToListAsync();

        return Ok(users);
    }

    // GET /admin/users/{userId}/roles
    [HttpGet("{userId:guid}/roles")]
    public async Task<IActionResult> GetUserRoles([FromRoute] Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return NotFound(new { message = "User not found." });

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(roles);
    }

    // POST /admin/users/{userId}/roles/{role}
    [HttpPost("{userId:guid}/roles/{role}")]
    public async Task<IActionResult> AddRole([FromRoute] Guid userId, [FromRoute] string role)
    {
        role = (role ?? "").Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(role))
            return BadRequest(new { message = "Role is required." });

        if (!await _roleManager.RoleExistsAsync(role))
            return BadRequest(new { message = $"Role '{role}' does not exist." });

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return NotFound(new { message = "User not found." });

        if (await _userManager.IsInRoleAsync(user, role))
            return NoContent();

        var result = await _userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return NoContent();
    }

    // DELETE /admin/users/{userId}/roles/{role}
    [HttpDelete("{userId:guid}/roles/{role}")]
    public async Task<IActionResult> RemoveRole([FromRoute] Guid userId, [FromRoute] string role)
    {
        role = (role ?? "").Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(role))
            return BadRequest(new { message = "Role is required." });

        if (!await _roleManager.RoleExistsAsync(role))
            return BadRequest(new { message = $"Role '{role}' does not exist." });

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return NotFound(new { message = "User not found." });

        if (!await _userManager.IsInRoleAsync(user, role))
            return NoContent();

        var result = await _userManager.RemoveFromRoleAsync(user, role);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return NoContent();
    }
}
