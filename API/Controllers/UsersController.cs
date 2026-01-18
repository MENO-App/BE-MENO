using Application.Common.Interfaces;
using Application.Features.Users.Commands;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
public class UsersController : ControllerBase
{

    private readonly IApplicationDBContext _db;

    public UsersController(IApplicationDBContext db)
    {
        _db = db;
    }

    //POST /create users
    [HttpPost]
    [Route("users")]
    public async Task<ActionResult<CreateUser.Response>> Create(
      [FromBody] CreateUser.Request request,
      CancellationToken ct)
    {
        var result = await CreateUser.Handle(_db, request, ct);
        return Created($"/users/{result.UserId}", result);
    }

    // GET /users/{id}
    // Returns a single user by id.
    [HttpGet]
    [Route("users/{id:guid}")]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var user = await _db.Users
            .Where(user => user.UserId == id)
            .Select(user => new
            {
                user.UserId,
                user.SchoolId,
                user.DisplayName,
                user.ClassGroup,
                user.Role,
                user.DefaultVegetarian,
                user.CreatedAt
            })
            .FirstOrDefaultAsync(ct);

        return user is null ? NotFound() : Ok(user);
    }


    // GET /schools/{schoolId}/users
    // Returns all users that belong to a specific school.
    [HttpGet]
    [Route("schools/{schoolId:guid}/users")]
    public async Task<IActionResult> GetBySchool(
        [FromRoute] Guid schoolId,
        CancellationToken ct)
    {
        var users = await _db.Users
          .Where(user => user.SchoolId == schoolId)
          .OrderBy(user => user.DisplayName)
          .Select(user => new
{
           user.UserId,
           user.SchoolId,
           user.DisplayName,
           user.ClassGroup,
           user.Role,
           user.DefaultVegetarian,
           user.CreatedAt
})
            .ToListAsync(ct);

        return Ok(users);
    }


    // PUT /users/{id}
    // Updates editable fields for a user.
    [HttpPut]
    [Route("users/{id:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(userEntity => userEntity.UserId == id, ct);

        if (user is null)
            return NotFound(new { message = "User not found." });

        user.DisplayName = request.DisplayName;
        user.ClassGroup = request.ClassGroup;
        user.DefaultVegetarian = request.DefaultVegetarian;
        user.Role = request.Role;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
    // DELETE /users/{id}
    // Deletes a user.
    [HttpDelete]
    [Route("users/{id:guid}")]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(userEntity => userEntity.UserId == id, ct);

        if (user is null)
            return NotFound(new { message = "User not found." });

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    // Request body for updating a user
    public sealed record UpdateUserRequest(
        string DisplayName,
        string ClassGroup,
        bool DefaultVegetarian,
        Role Role
    );
}