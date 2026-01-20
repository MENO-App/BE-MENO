using Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Auth;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateToken(Guid userId, string email, IList<string> roles)
    {
        // Read JWT configuration
        var jwtSection = _configuration.GetSection("Jwt");

        // Create signing key from secret
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSection["Key"]!)
        );

        // Base claims that identify the user
        var claims = new List<Claim>
        {
            // Subject = unique user identifier
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),

            // User email (useful for debugging and client apps)
            new(JwtRegisteredClaimNames.Email, email),

            // Unique token id
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add role claims (used by [Authorize(Roles = "...")])
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Create signing credentials using HMAC SHA256
        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        // Create the JWT token
        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                double.Parse(jwtSection["ExpiryMinutes"]!)
            ),
            signingCredentials: credentials
        );

        // Serialize token to string
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
