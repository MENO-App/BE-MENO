using Application.Common;
using MediatR;

namespace Application.Features.Allergies.Queries
{
    public record ListUserAllergiesQuery(Guid UserId) : IRequest<Result<IEnumerable<UserAllergyDto>>>;
    public record UserAllergyDto(Guid AllergyId, string Name, string Notes);
}
