using Microsoft.AspNetCore.Mvc;
using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
public class AllergiesController : ControllerBase
{
    private readonly IApplicationDBContext _db;

    public AllergiesController(IApplicationDBContext db)
    {
        _db = db;
    }

    // POST /allergies
    [HttpPost]
    [Route("allergies")]
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
    [Route("allergies")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var items = await _db.Allergies
            .OrderBy(a => a.Name)
            .Select(a => new { a.AllergyId, a.Name })
            .ToListAsync(ct);

        return Ok(items);
    }

    public sealed record CreateAllergyRequest(string Name);
}
