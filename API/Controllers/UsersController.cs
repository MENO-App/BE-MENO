using Application.Common.Interfaces;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;


namespace API.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]

public class UsersController : ControllerBase
{

    private readonly IApplicationDBContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(IApplicationDBContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // NOTE:
    // This controller is now backed by ASP.NET Identity (ApplicationUser).
    // Domain.Entities.User is no longer used for authentication/user management.

    // GET /users/{id}
    // Returns a single identity user by id.
    [HttpGet]
    [Route("users/{id:guid}")]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var user = await _userManager.Users
            .Where(u => u.Id == id)
            .Select(u => new
            {
                UserId = u.Id,
                u.Email
            })
            .FirstOrDefaultAsync(ct);

        return user is null ? NotFound() : Ok(user);
    }

    // GET /schools/{schoolId}/users
    // TEMPORARY: Identity users currently do not have SchoolId.
    // This returns all identity users until we add SchoolId to ApplicationUser
    // or introduce a UserProfile mapping.
    [HttpGet]
    [Route("schools/{schoolId:guid}/users")]
    public async Task<IActionResult> GetBySchool(
        [FromRoute] Guid schoolId,
        CancellationToken ct)
    {
        var users = await _userManager.Users
            .OrderBy(u => u.Email)
            .Select(u => new
            {
                UserId = u.Id,
                u.Email
            })
            .ToListAsync(ct);

        return Ok(users);
    }

    // PUT /users/{id}
    // Updates basic identity fields.
    [HttpPut]
    [Route("users/{id:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return NotFound(new { message = "User not found." });

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            user.Email = request.Email;
            user.UserName = request.Email;
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return BadRequest(updateResult.Errors);

        return NoContent();
    }

    // DELETE /users/{id}
    // Deletes an identity user.
    [HttpDelete]
    [Route("users/{id:guid}")]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return NotFound(new { message = "User not found." });

        var deleteResult = await _userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
            return BadRequest(deleteResult.Errors);

        return NoContent();
    }

    // Request body for updating a user
    public sealed record UpdateUserRequest(string? Email);
}
