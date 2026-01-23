using System.Security.Claims;
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

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy
                    .WithOrigins(
                        "http://localhost:5173",
                        "http://localhost:3000",
                        "http://localhost:8080"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });


        // -------------------------
        // Database
        // -------------------------
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        builder.Services.AddScoped<IApplicationDBContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        // -------------------------
        // Identity
        // -------------------------
        builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // Prevent cookie redirect (Swagger/clients should get 401 instead of redirect to /Account/Login)
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Events.OnRedirectToLogin = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
        });

        // -------------------------
        // AuthN (JWT)
        // -------------------------
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            var jwt = builder.Configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwt["Key"]!);

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwt["Issuer"],
                ValidAudience = jwt["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key),

                RoleClaimType = ClaimTypes.Role,
                NameClaimType = ClaimTypes.NameIdentifier,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });

        // -------------------------
        // AuthZ (policies)
        // -------------------------
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("ADMIN"));
            options.AddPolicy("KitchenOnly", policy => policy.RequireRole("KITCHEN"));
            options.AddPolicy("StudentOrStaff", policy => policy.RequireRole("STUDENT", "STAFF"));
        });

        // -------------------------
        // App services
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

        // (Optional but recommended) Force known URLs to avoid "Failed to determine the https port..."
        // If you only want HTTPS, keep only https line.
        app.Urls.Clear();
        app.Urls.Add("https://localhost:7292");
        app.Urls.Add("http://localhost:5169");

        // -------------------------
        // Seed data
        // -------------------------
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await IdentitySeeder.SeedRolesAsync(roleManager);

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

            if (!await db.Schools.AnyAsync())
            {
                db.Schools.Add(new School
                {
                    SchoolId = Guid.NewGuid(),
                    Name = "Default School",
                    Timezone = "Europe/Stockholm"
                });

                await db.SaveChangesAsync();
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
        app.UseRouting();
        app.UseCors("AllowFrontend");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
