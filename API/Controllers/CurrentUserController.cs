using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Application.Users.Allergies.Dtos;
using Microsoft.EntityFrameworkCore;


namespace API.Controllers;

[ApiController]
[Route("users/me")]
[Authorize] // All authenticated users (Admins + regular users)
public sealed class CurrentUserController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _config;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;


    public CurrentUserController(
     IUserRepository userRepository,
     IConfiguration config,
     ApplicationDbContext db,
     UserManager<ApplicationUser> userManager)
    {
        _userRepository = userRepository;
        _config = config;
        _db = db;
        _userManager = userManager;
    }


    // =========================
    // GET /users/me
    // Returns the current user's profile.
    // Automatically creates a profile on first login.
    // =========================
    [HttpGet]
    public async Task<IActionResult> GetMe()
    {
        var identityUserId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(identityUserId))
            return Unauthorized("Missing user id claim.");

        var user = await _userRepository.GetByIdentityUserIdAsync(identityUserId);

        // Auto-create domain user on first login
        if (user is null)
        {
            var defaultSchoolIdString = _config["Defaults:SchoolId"];

            if (!Guid.TryParse(defaultSchoolIdString, out var defaultSchoolId))
            {
                return Problem(
                    title: "Server configuration error",
                    detail: "Defaults:SchoolId is missing or invalid.",
                    statusCode: 500);
            }

            user = new User
            {
                UserId = Guid.NewGuid(),
                IdentityUserId = identityUserId,
                SchoolId = defaultSchoolId,
                Role = default(Role), // Domain role (NOT ASP.NET Identity role)
                DisplayName = string.Empty,
                ClassGroup = string.Empty,
                DefaultVegetarian = false,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();
        }

        return Ok(new
        {
            user.UserId,
            user.DisplayName,
            user.ClassGroup,
            user.DefaultVegetarian,
            user.SchoolId,
            user.Role,
            Allergies = user.UserAllergies.Select(ua => new
            {
                ua.AllergyId,
                AllergyName = ua.Allergy != null ? ua.Allergy.Name : string.Empty,
                Notes = ua.Notes
            })

        });
    }

    // =========================
    // PUT /users/me
    // Updates the current user's profile information.
    // =========================
    public sealed record UpdateMyProfileDto(
        string DisplayName,
        string ClassGroup,
        bool DefaultVegetarian,
        Guid? SchoolId
    );

    [HttpPut]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateMyProfileDto dto)
    {
        var identityUserId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(identityUserId))
            return Unauthorized();

        var user = await _userRepository.GetByIdentityUserIdAsync(identityUserId);
        if (user is null)
            return NotFound();

        user.DisplayName = dto.DisplayName ?? string.Empty;
        user.ClassGroup = dto.ClassGroup ?? string.Empty;
        user.DefaultVegetarian = dto.DefaultVegetarian;

        if (dto.SchoolId.HasValue)
            user.SchoolId = dto.SchoolId.Value;

        await _userRepository.SaveChangesAsync();
        return NoContent(); // 204
    }

    // =========================
    // PUT /users/me/email
    // Updates the current user's email.
    // =========================
    public sealed record UpdateEmailDto(string Email);

    [HttpPut("email")]
    public async Task<IActionResult> UpdateEmail([FromBody] UpdateEmailDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest(new { message = "Email is required." });

        var identityUserId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(identityUserId))
            return Unauthorized();

        var identityUser = await _userManager.FindByIdAsync(identityUserId);
        if (identityUser is null)
            return NotFound();

        identityUser.Email = dto.Email.Trim();
        identityUser.UserName = dto.Email.Trim();

        var result = await _userManager.UpdateAsync(identityUser);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return NoContent();
    }

    // =========================
    // POST /users/me/change-password
    // Changes the current user's password.
    // =========================
    public sealed record ChangePasswordDto(string CurrentPassword, string NewPassword);

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CurrentPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
            return BadRequest(new { message = "Current and new password are required." });

        var identityUserId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(identityUserId))
            return Unauthorized();

        var identityUser = await _userManager.FindByIdAsync(identityUserId);
        if (identityUser is null)
            return NotFound();

        var result = await _userManager.ChangePasswordAsync(identityUser, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return NoContent();
    }

    // =========================
    // GET /users/me/allergies
    // Returns all allergy IDs for the current user.
    // =========================
    [HttpGet("allergies")]
    public async Task<IActionResult> GetMyAllergies()
    {
        var identityUserId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(identityUserId))
            return Unauthorized();

        var user = await _userRepository.GetByIdentityUserIdAsync(identityUserId);
        if (user is null)
            return NotFound();

        return Ok(user.UserAllergies.Select(ua => ua.AllergyId));
    }

    // =========================
    // PUT /users/me/allergies
    // Replaces all user allergies with the provided list.
    // =========================
    public sealed record UpdateMyAllergiesDto(List<Guid> AllergyIds);

    [HttpPut("allergies")]
    public async Task<IActionResult> UpdateMyAllergies([FromBody] UpdateMyAllergiesDto dto)
    {
        var identityUserId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(identityUserId))
            return Unauthorized();

        var user = await _userRepository.GetByIdentityUserIdAsync(identityUserId);
        if (user is null)
            return NotFound();

        // Replace all existing allergies
        user.UserAllergies.Clear();

        foreach (var allergyId in dto.AllergyIds.Distinct())
        {
            user.UserAllergies.Add(new UserAllergy
            {
                UserId = user.UserId,
                AllergyId = allergyId
            });
        }

        await _userRepository.SaveChangesAsync();
        return NoContent(); // 204
    }

    // =========================
    // POST /users/me/allergies
    // Adds a single allergy to the current user.
    // Used when selecting allergies one-by-one (e.g. including "Other").
    // =========================
    [HttpPost("allergies")]
    public async Task<IActionResult> AddAllergy([FromBody] AddUserAllergyRequest request)
    {
        var identityUserId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(identityUserId))
            return Unauthorized("Missing user id claim.");

        var user = await _userRepository.GetByIdentityUserIdAsync(identityUserId);
        if (user is null)
            return NotFound("User profile not found.");

        var allergyExists = await _db.Allergies.AnyAsync(a => a.AllergyId == request.AllergyId);
        if (!allergyExists)
            return BadRequest("Unknown AllergyId.");

        var existingLink = await _db.UserAllergies
            .FirstOrDefaultAsync(x =>
                x.UserId == user.UserId && x.AllergyId == request.AllergyId);

        if (existingLink is not null)
        {
            existingLink.Notes = request.Notes?.Trim() ?? string.Empty;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        _db.UserAllergies.Add(new UserAllergy
        {
            UserId = user.UserId,
            AllergyId = request.AllergyId,
            Notes = request.Notes?.Trim() ?? string.Empty
        });

        await _db.SaveChangesAsync();
        return NoContent();
    }

}
