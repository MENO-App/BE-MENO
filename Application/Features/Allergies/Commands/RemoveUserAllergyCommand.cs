using Application.Common;
using MediatR;

namespace Application.Features.Allergies.Commands
{
    public record RemoveUserAllergyCommand(Guid UserId, Guid AllergyId) : IRequest<Result>;
}
