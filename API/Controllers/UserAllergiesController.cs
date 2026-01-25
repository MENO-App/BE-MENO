using Application.Common.Interfaces;
using Application.Users.Allergies.Dtos;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("users/{id:guid}/allergies")]
public sealed class UserAllergiesController : ControllerBase
{
    private readonly IApplicationDBContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserAllergiesController(IApplicationDBContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // Helper: map Identity userId (AspNetUsers.Id) -> Domain User (dbo.User.UserId)
    private async Task<User?> GetOrCreateDomainUserAsync(Guid identityUserId, CancellationToken ct)
    {
        // 1) Identity user must exist
        var identityUser = await _userManager.FindByIdAsync(identityUserId.ToString());
        if (identityUser is null) return null;

        // 2) Try find domain user profile by IdentityUserId
        var domainUser = await _db.Users
            .FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId.ToString(), ct);

        if (domainUser is not null) return domainUser;

        // 3) Create domain user profile (minimal defaults)
        var schoolId = await _db.Schools
            .Select(s => s.SchoolId)
            .FirstOrDefaultAsync(ct);

        if (schoolId == Guid.Empty)
            return null; // no school seeded/created yet

        domainUser = new User
        {
            UserId = Guid.NewGuid(),
            IdentityUserId = identityUserId.ToString(),
            SchoolId = schoolId,
            Role = Role.User,
            DisplayName = identityUser.Email ?? "User",
            ClassGroup = string.Empty,
            DefaultVegetarian = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(domainUser);
        await _db.SaveChangesAsync(ct);

        return domainUser;
    }

    // GET /users/{id}/allergies
    [HttpGet]
    public async Task<IActionResult> GetAllergies([FromRoute] Guid id, CancellationToken ct)
    {
        var domainUser = await GetOrCreateDomainUserAsync(id, ct);
        if (domainUser is null)
            return NotFound(new { message = "User profile not found (missing identity user or school)." });

        var allergies = await _db.UserAllergies
            .Where(ua => ua.UserId == domainUser.UserId)
            .Join(_db.Allergies,
                ua => ua.AllergyId,
                a => a.AllergyId,
                (ua, a) => new
                {
                    a.AllergyId,
                    a.Name,
                    ua.Notes
                })
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

        return Ok(allergies);
    }

    // POST /users/{id}/allergies
    [HttpPost]
    public async Task<IActionResult> AddAllergy([FromRoute] Guid id, [FromBody] AddUserAllergyRequest request, CancellationToken ct)
    {
        if (request is null)
            return BadRequest(new { message = "Request body is required." });

        if (request.AllergyId == Guid.Empty)
            return BadRequest(new { message = "AllergyId is required." });

        var domainUser = await GetOrCreateDomainUserAsync(id, ct);
        if (domainUser is null)
            return NotFound(new { message = "User profile not found (missing identity user or school)." });

        var allergyExists = await _db.Allergies.AnyAsync(a => a.AllergyId == request.AllergyId, ct);
        if (!allergyExists)
            return NotFound(new { message = "Allergy not found." });

        var existing = await _db.UserAllergies
            .FirstOrDefaultAsync(ua => ua.UserId == domainUser.UserId && ua.AllergyId == request.AllergyId, ct);

        if (existing is not null)
        {
            existing.Notes = request.Notes?.Trim() ?? string.Empty;
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        _db.UserAllergies.Add(new UserAllergy
        {
            UserId = domainUser.UserId,
            AllergyId = request.AllergyId,
            Notes = request.Notes?.Trim() ?? string.Empty
        });

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE /users/{id}/allergies/{allergyId}
    [HttpDelete("{allergyId:guid}")]
    public async Task<IActionResult> RemoveAllergy([FromRoute] Guid id, [FromRoute] Guid allergyId, CancellationToken ct)
    {
        var domainUser = await GetOrCreateDomainUserAsync(id, ct);
        if (domainUser is null)
            return NotFound(new { message = "User profile not found (missing identity user or school)." });

        var link = await _db.UserAllergies
            .FirstOrDefaultAsync(ua => ua.UserId == domainUser.UserId && ua.AllergyId == allergyId, ct);

        if (link is null)
            return NotFound(new { message = "User does not have this allergy." });

        _db.UserAllergies.Remove(link);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}
