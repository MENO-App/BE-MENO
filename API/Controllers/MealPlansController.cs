using Microsoft.AspNetCore.Mvc;
using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Enums;




namespace API.Controllers;

[ApiController]
public class MealPlansController : ControllerBase
{
    private readonly IApplicationDBContext _db;

    public MealPlansController(IApplicationDBContext db)
    {
        _db = db;
    }

    // Helper method for strict date
    private static bool TryParseDate(string value, out DateOnly date)
    {
        return DateOnly.TryParseExact(value, "yyyy-MM-dd", out date);
    }

    // Represents the input data for upserting a meal plan.
    // Used by PUT /users/{id}/mealplans/{date}
    public sealed record UpsertMealPlanRequest(
    MealChoiceStatus Status,
    bool WantsVegetarian
);

    // PUT /users/{id}/mealplans/{date}
    [HttpPut]
    [Route("users/{id:guid}/mealplans/{date}")]
    public async Task<IActionResult> UpsertMealPlan(
    [FromRoute] Guid id,
    [FromRoute] string date,
    [FromBody] UpsertMealPlanRequest request,
    CancellationToken ct)
    {
        if (!TryParseDate(date, out var parsedDate))
            return BadRequest(new { message = "Invalid date format. Use yyyy-MM-dd." });

        var exists = await _db.MealPlans
            .FirstOrDefaultAsync(x => x.UserId == id && x.Date == parsedDate, ct);

        if (exists is null)
        {
            _db.MealPlans.Add(new MealPlan
            {
                UserId = id,
                Date = parsedDate,
                Status = request.Status,
                WantsVegetarian = request.WantsVegetarian
            });
        }
        else
        {
            exists.Status = request.Status;
            exists.WantsVegetarian = request.WantsVegetarian;
        }

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }


    // GET /users/{id}/mealplans/{date}
    // Returns the meal plan for a specific user on a specific date.
    [HttpGet]
    [Route("users/{id:guid}/mealplans/{date}")]
    public async Task<IActionResult> GetMealPlanByDate(
     [FromRoute] Guid id,
     [FromRoute] string date,
     CancellationToken ct)
    {
        // Enforce strict date format (yyyy-MM-dd) to avoid ambiguity
        // This prevents issues with locale-specific date formats
        if (!TryParseDate(date, out var d))
            return BadRequest(new { message = "Invalid date format. Use yyyy-MM-dd." });

        // Query the database for a meal plan matching the user and date
        var item = await _db.MealPlans
            .Where(x => x.UserId == id && x.Date == d)
            .Select(x => new
            {
                x.UserId,
                // Convert DateOnly back to string for a clean API response
                Date = x.Date.ToString("yyyy-MM-dd"),
                x.Status,
                x.WantsVegetarian
            })
            .FirstOrDefaultAsync(ct);

        // If no meal plan exists for that date, return 404
        return item is null ? NotFound() : Ok(item);
    }

    // GET /users/{id}/mealplans?from=...&to=...
    // Returns all meal plans for a user within a given date range.
    [HttpGet]
    [Route("users/{id:guid}/mealplans")]
    public async Task<IActionResult> GetMealPlansRange(
      [FromRoute] Guid id,
      [FromQuery] string from,
      [FromQuery] string to,
      CancellationToken ct)
    {
        // Validate both query parameters and ensure correct date format
        if (!TryParseDate(from, out var fromDate) || !TryParseDate(to, out var toDate))
            return BadRequest(new { message = "Invalid date format. Use yyyy-MM-dd for from/to." });

        // Ensure logical date range
        if (toDate < fromDate)
            return BadRequest(new { message = "`to` must be >= `from`." });

        // Fetch all meal plans within the date range, ordered by date
        var items = await _db.MealPlans
            .Where(x => x.UserId == id && x.Date >= fromDate && x.Date <= toDate)
            .OrderBy(x => x.Date)
            .Select(x => new
            {
                x.UserId,
                Date = x.Date.ToString("yyyy-MM-dd"),
                x.Status,
                x.WantsVegetarian
            })
            .ToListAsync(ct);

        // Always return 200 with a list (empty list if none exist)
        return Ok(items);
    }
}
