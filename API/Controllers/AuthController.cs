using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity.Data;
using Application.Common;
using Azure.Core;
using Domain.Entities;


namespace API.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(UserManager<ApplicationUser> userManager, IJwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
    }

    // POST /auth/register
    // POST /auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required." });

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        // ✅ Assign default role to new users
        var roleResult = await _userManager.AddToRoleAsync(user, "STUDENT");
        if (!roleResult.Succeeded)
            return BadRequest(roleResult.Errors);

        return Created($"/users/{user.Id}", new { UserId = user.Id, user.Email });
    }


    public sealed record RegisterRequest(string Email, string Password);

    // POST /auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required." });

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized(new { message = "Invalid credentials." });

        var passwordOk = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordOk)
            return Unauthorized(new { message = "Invalid credentials." });

        var roles = await _userManager.GetRolesAsync(user);

        // Create JWT 
        var token = _jwtTokenService.CreateToken(user.Id, user.Email!, roles);

        return Ok(new
        {
            accessToken = token,
            userId = user.Id,
            email = user.Email,
            roles
        });
    }
    public sealed record LoginRequest(string Email, string Password);


}
