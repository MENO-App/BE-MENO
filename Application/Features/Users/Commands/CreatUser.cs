using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Features.Users.Commands
{
    public static class CreateUser
    {
        public sealed record Request(
            Guid SchoolId,
            Role Role,
            string DisplayName,
            string ClassGroup,
            bool DefaultVegetarian
        );

        public sealed record Response(Guid UserId);

        public static async Task<Response> Handle(
            IApplicationDBContext db,
            Request request,
            CancellationToken ct = default)
        {
            var user = new User
            {
                UserId = Guid.NewGuid(),
                SchoolId = request.SchoolId,
                Role = request.Role,
                DisplayName = request.DisplayName,
                ClassGroup = request.ClassGroup,
                DefaultVegetarian = request.DefaultVegetarian,
                CreatedAt = DateTime.UtcNow
            };

            db.Users.Add(user);
            await db.SaveChangesAsync(ct);

            return new Response(user.UserId);
        }
    }
}
