using System.Text;
using Application.Common.Interfaces;
using Domain.Entities;
using FluentValidation;
using Infrastructure.Auth;
using Infrastructure.Data;
using Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace API;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // -------------------------
        // Database + Application DB abstraction
        // -------------------------
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        builder.Services.AddScoped<IApplicationDBContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        // -------------------------
        // Identity (users + roles)
        // -------------------------
        builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // -------------------------
        // JWT Authentication
        // -------------------------
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtSection = builder.Configuration.GetSection("Jwt");
                var key = Encoding.UTF8.GetBytes(jwtSection["Key"]!);

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidAudience = jwtSection["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        // -------------------------
        // Authorization (policies)
        // -------------------------
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("ADMIN"));
            options.AddPolicy("KitchenOnly", policy => policy.RequireRole("KITCHEN"));
            options.AddPolicy("StudentOrStaff", policy => policy.RequireRole("STUDENT", "STAFF"));
        });

        // -------------------------
        // Application services
        // -------------------------
        builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

        // -------------------------
        // MediatR + FluentValidation + pipeline behavior
        // -------------------------
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Application.Common.Result).Assembly));
        builder.Services.AddValidatorsFromAssembly(typeof(Application.Common.Result).Assembly);

        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Application.Common.ValidationBehavior<,>));
        // -------------------------
        // Controllers + Swagger
        // -------------------------
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            // Swagger "Authorize" button for Bearer JWT
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter: Bearer {your JWT token}"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        var app = builder.Build();

        // -------------------------
        // Seed data (roles + initial admin + default school)
        // -------------------------
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // 1) Seed roles
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            await IdentitySeeder.SeedRolesAsync(roleManager);

            // 2) Seed initial admin (from appsettings.json -> InitialAdmin)
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var adminSection = builder.Configuration.GetSection("InitialAdmin");
            var adminEmail = adminSection["Email"];
            var adminPassword = adminSection["Password"];

            if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
            {
                await IdentitySeeder.SeedInitialAdminAsync(
                    userManager,
                    roleManager,
                    adminEmail,
                    adminPassword
                );
            }

            // 3) Seed default school
            if (!db.Schools.Any())
            {
                db.Schools.Add(new School
                {
                    SchoolId = Guid.NewGuid(),
                    Name = "Default School",
                    Timezone = "Europe/Stockholm"
                });

                db.SaveChanges();
            }
        }


        // -------------------------
        // HTTP pipeline
        // -------------------------
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        // IMPORTANT ORDER:
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
