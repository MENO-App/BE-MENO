using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("admin")]
[Authorize(Policy = "AdminOnly")]
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

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        // Simple list to help admins pick correct IDs in Swagger
        var users = await _userManager.Users
            .OrderBy(u => u.Email)
            .Select(u => new
            {
                userId = u.Id,
                email = u.Email
            })
            .ToListAsync();

        return Ok(users);
    }

    // POST /admin/users/{userId}/roles/{role}
    // Adds a role to a user (ADMIN only).
    [HttpPost("users/{userId:guid}/roles/{role}")]
    public async Task<IActionResult> AddRoleToUser([FromRoute] Guid userId, [FromRoute] string role)
    {
        role = role.Trim().ToUpperInvariant();

        // Ensure role exists
        var roleExists = await _roleManager.RoleExistsAsync(role);
        if (!roleExists)
            return BadRequest(new { message = $"Role '{role}' does not exist." });

        // Find user
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return NotFound(new { message = "User not found." });

        // Add role if not already assigned
        if (await _userManager.IsInRoleAsync(user, role))
            return NoContent();

        var result = await _userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return NoContent();
    }
}
