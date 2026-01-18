using Microsoft.AspNetCore.Mvc;
using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
public class SchoolsController : ControllerBase
{
    private readonly IApplicationDBContext _db;

    public SchoolsController(IApplicationDBContext db)
    {
        _db = db;
    }

    // POST /schools
    // Creates a new school.
    [HttpPost]
    [Route("schools")]
    public async Task<IActionResult> Create(
        [FromBody] CreateSchoolRequest request,
        CancellationToken ct)
    {
        var school = new School
        {
            SchoolId = Guid.NewGuid(),
            Name = request.Name,
            Timezone = request.Timezone ?? "Europe/Stockholm"
        };

        _db.Schools.Add(school);
        await _db.SaveChangesAsync(ct);

        return Created($"/schools/{school.SchoolId}", new
        {
            school.SchoolId,
            school.Name,
            school.Timezone
        });
    }

    // GET /schools
    // Returns all schools.
    [HttpGet]
    [Route("schools")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var schools = await _db.Schools
            .OrderBy(school => school.Name)
            .Select(school => new
            {
                school.SchoolId,
                school.Name,
                school.Timezone
            })
            .ToListAsync(ct);

        return Ok(schools);
    }

    public sealed record CreateSchoolRequest(string Name, string? Timezone);
}
