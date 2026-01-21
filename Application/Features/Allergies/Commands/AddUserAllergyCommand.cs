using Application.Common;
using MediatR;


namespace Application.Features.Allergies.Commands
{
    public record AddUserAllergyCommand(Guid UserId, Guid AllergyId, string? Notes) : IRequest<Result>;
}
