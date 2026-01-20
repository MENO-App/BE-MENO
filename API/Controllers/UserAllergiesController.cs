using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;


namespace API.Controllers;

[ApiController]
public class UserAllergiesController : ControllerBase
{
    private readonly IApplicationDBContext _db;
    private readonly UserManager<ApplicationUser> _userManager;


    public UserAllergiesController(IApplicationDBContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }


    // POST /users/{id}/allergies
    // Adds a link between a user and an allergy.
    [HttpPost]
    [Route("users/{id:guid}/allergies")]
    public async Task<IActionResult> AddAllergy(
        [FromRoute] Guid id,
        [FromBody] AddUserAllergyRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest(new { message = "Request body is required." });

        if (request.AllergyId == Guid.Empty)
            return BadRequest(new { message = "AllergyId is required." });

        bool userExists = await _userManager.Users.AnyAsync(u => u.Id == id, cancellationToken);
        if (!userExists)
            return NotFound(new { message = "User not found." });

        bool allergyExists = await _db.Allergies.AnyAsync(allergyEntity => allergyEntity.AllergyId == request.AllergyId, cancellationToken);
        if (!allergyExists)
            return NotFound(new { message = "Allergy not found." });

        bool linkExists = await _db.UserAllergies.AnyAsync(linkEntity =>
            linkEntity.UserId == id && linkEntity.AllergyId == request.AllergyId, cancellationToken);

        if (linkExists)
            return Conflict(new { message = "User already has this allergy." });

        var newLink = new UserAllergy
        {
            UserId = id,
            AllergyId = request.AllergyId,
            Notes = request.Notes
        };

        _db.UserAllergies.Add(newLink);
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // DELETE /users/{id}/allergies/{allergyId}
    // Removes the link between a user and an allergy.
    [HttpDelete]
    [Route("users/{id:guid}/allergies/{allergyId:guid}")]
    public async Task<IActionResult> RemoveAllergy(
        [FromRoute] Guid id,
        [FromRoute] Guid allergyId,
        CancellationToken cancellationToken)
    {
        bool userExists = await _userManager.Users.AnyAsync(u => u.Id == id, cancellationToken);
        if (!userExists)
            return NotFound(new { message = "User not found." });

        bool allergyExists = await _db.Allergies.AnyAsync(allergyEntity => allergyEntity.AllergyId == allergyId, cancellationToken);
        if (!allergyExists)
            return NotFound(new { message = "Allergy not found." });

        var link = await _db.UserAllergies
            .FirstOrDefaultAsync(linkEntity => linkEntity.UserId == id && linkEntity.AllergyId == allergyId, cancellationToken);

        if (link is null)
            return NotFound(new { message = "User does not have this allergy." });

        _db.UserAllergies.Remove(link);
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // GET /users/{id}/allergies
    // Returns all allergies for a user.
    [HttpGet]
    [Route("users/{id:guid}/allergies")]
    public async Task<IActionResult> GetAllergies(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        bool userExists = await _userManager.Users.AnyAsync(u => u.Id == id, cancellationToken);
        if (!userExists)
            return NotFound(new { message = "User not found." });

        var allergies = await _db.UserAllergies
            .Where(linkEntity => linkEntity.UserId == id)
            .Join(_db.Allergies,
                linkEntity => linkEntity.AllergyId,
                allergyEntity => allergyEntity.AllergyId,
                (linkEntity, allergyEntity) => new
                {
                    allergyEntity.AllergyId,
                    allergyEntity.Name,
                    linkEntity.Notes
                })
            .OrderBy(result => result.Name)
            .ToListAsync(cancellationToken);

        return Ok(allergies);
    }

    // Request body for POST /users/{id}/allergies
    public sealed record AddUserAllergyRequest(Guid AllergyId, string? Notes);
}
