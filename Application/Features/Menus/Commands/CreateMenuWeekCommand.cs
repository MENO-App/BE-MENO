using Application.Common;
using MediatR;

namespace Application.Features.Menus.Commands
{
    public record CreateMenuWeekCommand(Guid SchoolId, int Year, int WeekNumber) : IRequest<Result<Guid>>;
}
