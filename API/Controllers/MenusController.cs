using Microsoft.AspNetCore.Mvc;
using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Domain.Enums;

namespace API.Controllers;

[ApiController]
public class MenusController : ControllerBase
{
    private readonly IApplicationDBContext _db;

    public MenusController(IApplicationDBContext db)
    {
        _db = db;
    }

    // Request body for replacing all allergens on a menu item.
    // Used by PUT /menuitems/{menuItemId}/allergens
    public sealed record UpdateMenuItemAllergensRequest(List<string> Allergens);


    // Request body for creating a new menu week
    public sealed record CreateMenuWeekRequest(int Year, int WeekNumber);

    // Request body for adding a menu item to a menu week
    public sealed record CreateMenuItemRequest(
        int DayOfWeek,
        MenuItemType Type,
        string Title,
        string? Description
    );

    //Request body for updating a menu item
    public sealed record UpdateMenuItemRequest(
    int DayOfWeek,
    MenuItemType Type,
    string Title,
    string? Description
);

    // POST /schools/{schoolId}/menuweeks
    // Creates a new menu week for a school (year + week number).
    [HttpPost]
    [Route("schools/{schoolId:guid}/menuweeks")]
    public async Task<IActionResult> CreateMenuWeek(
        [FromRoute] Guid schoolId,
        [FromBody] CreateMenuWeekRequest request,
        CancellationToken ct)
    {
        // Ensure the school exists (avoid foreign key errors)
        bool schoolExists = await _db.Schools.AnyAsync(school => school.SchoolId == schoolId, ct);
        if (!schoolExists)
            return NotFound(new { message = "School not found." });

        // Prevent duplicates: same school + year + week should be unique
        bool menuWeekAlreadyExists = await _db.MenuWeeks.AnyAsync(menuWeek =>
            menuWeek.SchoolId == schoolId &&
            menuWeek.Year == request.Year &&
            menuWeek.WeekNumber == request.WeekNumber, ct);

        if (menuWeekAlreadyExists)
            return Conflict(new { message = "MenuWeek already exists for that school/year/week." });

        var newMenuWeek = new MenuWeek
        {
            MenuWeekId = Guid.NewGuid(),
            SchoolId = schoolId,
            Year = request.Year,
            WeekNumber = request.WeekNumber,
            PublishedAt = null
        };

        _db.MenuWeeks.Add(newMenuWeek);
        await _db.SaveChangesAsync(ct);

        return Created(
            $"/schools/{schoolId}/menuweeks/{newMenuWeek.Year}/{newMenuWeek.WeekNumber}",
            new
            {
                newMenuWeek.MenuWeekId,
                newMenuWeek.SchoolId,
                newMenuWeek.Year,
                newMenuWeek.WeekNumber,
                newMenuWeek.PublishedAt
            }
        );
    }

    // POST /menuweeks/{menuWeekId}/publish
    // Marks a menu week as published by setting PublishedAt timestamp.
    [HttpPost]
    [Route("menuweeks/{menuWeekId:guid}/publish")]
    public async Task<IActionResult> PublishMenuWeek(
        [FromRoute] Guid menuWeekId,
        CancellationToken ct)
    {
        var menuWeek = await _db.MenuWeeks
            .FirstOrDefaultAsync(menuWeekEntity => menuWeekEntity.MenuWeekId == menuWeekId, ct);

        if (menuWeek is null)
            return NotFound(new { message = "MenuWeek not found." });

        // Idempotent behavior: if already published, keep the original timestamp
        menuWeek.PublishedAt ??= DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // GET /schools/{schoolId}/menuweeks/{year}/{week}
    // Returns a menu week with its menu items (and allergens) for a given school + year + week.
    [HttpGet]
    [Route("schools/{schoolId:guid}/menuweeks/{year:int}/{week:int}")]
    public async Task<IActionResult> GetMenuWeek(
        [FromRoute] Guid schoolId,
        [FromRoute] int year,
        [FromRoute] int week,
        CancellationToken ct)
    {
        var menuWeek = await _db.MenuWeeks
            .Where(menuWeekEntity =>
                menuWeekEntity.SchoolId == schoolId &&
                menuWeekEntity.Year == year &&
                menuWeekEntity.WeekNumber == week)
            .Select(menuWeekEntity => new
            {
                menuWeekEntity.MenuWeekId,
                menuWeekEntity.SchoolId,
                menuWeekEntity.Year,
                menuWeekEntity.WeekNumber,
                menuWeekEntity.PublishedAt,

                Items = menuWeekEntity.MenuItems.Select(menuItemEntity => new
                {
                    menuItemEntity.MenuItemId,
                    menuItemEntity.DayOfWeek,
                    menuItemEntity.Type,
                    menuItemEntity.Title,
                    menuItemEntity.Description,

                    Allergens = menuItemEntity.Allergens.Select(allergenEntity => new
                    {
                        allergenEntity.AllergenCode
                    })
                })
            })
            .FirstOrDefaultAsync(ct);

        return menuWeek is null ? NotFound() : Ok(menuWeek);
    }

    // POST /menuweeks/{menuWeekId}/items
    // Adds a menu item to a menu week.
    [HttpPost]
    [Route("menuweeks/{menuWeekId:guid}/items")]
    public async Task<IActionResult> AddMenuItem(
        [FromRoute] Guid menuWeekId,
        [FromBody] CreateMenuItemRequest request,
        CancellationToken ct)
    {
        bool menuWeekExists = await _db.MenuWeeks.AnyAsync(menuWeekEntity => menuWeekEntity.MenuWeekId == menuWeekId, ct);
        if (!menuWeekExists)
            return NotFound(new { message = "MenuWeek not found." });

        var newMenuItem = new MenuItem
        {
            MenuItemId = Guid.NewGuid(),
            MenuWeekId = menuWeekId,
            DayOfWeek = request.DayOfWeek,
            Type = request.Type,
            Title = request.Title,
            Description = request.Description
        };

        _db.MenuItems.Add(newMenuItem);
        await _db.SaveChangesAsync(ct);

        return Created(
            $"/menuitems/{newMenuItem.MenuItemId}",
            new
            {
                newMenuItem.MenuItemId,
                newMenuItem.MenuWeekId,
                newMenuItem.DayOfWeek,
                newMenuItem.Type,
                newMenuItem.Title,
                newMenuItem.Description
            }
        );
    }

    // PUT /menuitems/{menuItemId}
    // Updates the editable fields of a menu item.
    [HttpPut]
    [Route("menuitems/{menuItemId:guid}")]
    public async Task<IActionResult> UpdateMenuItem(
        [FromRoute] Guid menuItemId,
        [FromBody] UpdateMenuItemRequest request,
        CancellationToken ct)
    {
        var menuItem = await _db.MenuItems
            .FirstOrDefaultAsync(item => item.MenuItemId == menuItemId, ct);

        if (menuItem is null)
            return NotFound(new { message = "MenuItem not found." });

        menuItem.DayOfWeek = request.DayOfWeek;
        menuItem.Type = request.Type;
        menuItem.Title = request.Title;
        menuItem.Description = request.Description ?? string.Empty;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }


    // DELETE /menuitems/{menuItemId}
    // Deletes a menu item.
    [HttpDelete]
    [Route("menuitems/{menuItemId:guid}")]
    public async Task<IActionResult> DeleteMenuItem(
        [FromRoute] Guid menuItemId,
        CancellationToken ct)
    {
        var menuItem = await _db.MenuItems
            .FirstOrDefaultAsync(item => item.MenuItemId == menuItemId, ct);

        if (menuItem is null)
            return NotFound(new { message = "MenuItem not found." });

        _db.MenuItems.Remove(menuItem);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }


    // PUT /menuitems/{menuItemId}/allergens
    // Replaces the allergens list for a menu item (full replace).
    [HttpPut]
    [Route("menuitems/{menuItemId:guid}/allergens")]
    public async Task<IActionResult> UpdateMenuItemAllergens(
        [FromRoute] Guid menuItemId,
        [FromBody] UpdateMenuItemAllergensRequest request,
        CancellationToken ct)
    {
        var menuItem = await _db.MenuItems
            .Include(item => item.Allergens)
            .FirstOrDefaultAsync(item => item.MenuItemId == menuItemId, ct);

        if (menuItem is null)
            return NotFound(new { message = "MenuItem not found." });

        // Normalize input (trim + uppercase) and remove duplicates
        var allergenCodes = (request.Allergens ?? new List<string>())
            .Select(code => code.Trim().ToUpperInvariant())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct()
            .ToList();

        // Replace: remove all existing links
        menuItem.Allergens.Clear();

        // Add new links
        foreach (var allergenCode in allergenCodes)
        {
            menuItem.Allergens.Add(new MenuItemAllergen
            {
                MenuItemId = menuItemId,
                AllergenCode = allergenCode
            });
        }

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

}
