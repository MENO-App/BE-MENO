using Application.Common;
using MediatR;

namespace Application.Features.Allergies.Queries
{
    public record ListAllergiesQuery() : IRequest<Result<IEnumerable<AllergyDto>>>;
    public record AllergyDto(System.Guid AllergyId, string Name);
}
