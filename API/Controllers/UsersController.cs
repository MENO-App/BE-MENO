using Microsoft.AspNetCore.Mvc;
using Application.Common.Interfaces;
using Application.Features.Users.Commands;

namespace API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IApplicationDBContext _db;

    public UsersController(IApplicationDBContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<ActionResult<CreateUser.Response>> Create(
        [FromBody] CreateUser.Request request,
        CancellationToken ct)
    {
        var result = await CreateUser.Handle(_db, request, ct);
        return CreatedAtRoute(nameof(GetById), new { id = result.UserId }, result);
    }

    // placeholder until real query
    [HttpGet("{id:guid}", Name = nameof(GetById))]
    public IActionResult GetById(Guid id) => Ok(new { id });
}
