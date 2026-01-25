using Application.Common;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Users.Queries
{
    public record GetUserByIdQuery(Guid Id) : IRequest<Result<UserDto>>;

    public record UserDto(Guid UserId, Guid SchoolId, string DisplayName, string ClassGroup, bool DefaultVegetarian, string Role, DateTime CreatedAt, IEnumerable<UserAllergyDto> Allergies);
    public record UserAllergyDto(Guid AllergyId, string Name, string Notes);
}
