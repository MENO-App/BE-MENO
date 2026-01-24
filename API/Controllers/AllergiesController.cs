using Microsoft.AspNetCore.Mvc;
using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;


namespace API.Controllers;

[ApiController]
[Route("allergies")]
public sealed class AllergiesController : ControllerBase
{
    private readonly IApplicationDBContext _db;

    public AllergiesController(IApplicationDBContext db)
    {
        _db = db;
    }

    // POST /allergies
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Create([FromBody] CreateAllergyRequest request, CancellationToken ct)

    {
        var allergy = new Allergy
        {
            AllergyId = Guid.NewGuid(),
            Name = request.Name.Trim()
        };

        _db.Allergies.Add(allergy);
        await _db.SaveChangesAsync(ct);

        return Created($"/allergies/{allergy.AllergyId}", new { allergy.AllergyId, allergy.Name });
    }

    // GET /allergies
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var items = await _db.Allergies
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .Select(a => new { a.AllergyId, a.Name })
            .ToListAsync(ct);

        return Ok(items);
    }


    public sealed record CreateAllergyRequest(string Name);
}
