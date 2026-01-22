using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("users/me")]
[Authorize] // Alla inloggade användare (Admin + barn/users)
public sealed class CurrentUserController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _config;

    public CurrentUserController(
        IUserRepository userRepository,
        IConfiguration config)
    {
        _userRepository = userRepository;
        _config = config;
    }

    // =========================
    // GET /users/me
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

        // Auto-create profile on first login
        if (user is null)
        {
            var defaultSchoolIdString = _config["Defaults:SchoolId"];

            if (!Guid.TryParse(defaultSchoolIdString, out var defaultSchoolId))
            {
                return Problem(
                    title: "Server configuration missing",
                    detail: "Defaults:SchoolId is missing or invalid.",
                    statusCode: 500);
            }

            user = new User
            {
                UserId = Guid.NewGuid(),
                IdentityUserId = identityUserId,
                SchoolId = defaultSchoolId,
                Role = default(Role), // Domain role (NOT Identity role)
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
                AllergyName = ua.Allergy.Name
            })
        });
    }

    // =========================
    // PUT /users/me
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
    // GET /users/me/allergies
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

        // Replace all allergies
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
}
