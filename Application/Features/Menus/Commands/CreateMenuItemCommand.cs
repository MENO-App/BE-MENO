using Application.Common;
using Domain.Enums;
using MediatR;

namespace Application.Features.Menus.Commands
{
    public record CreateMenuItemCommand(Guid MenuWeekId, int DayOfWeek, MenuItemType Type, string Title, string? Description) : IRequest<Result<Guid>>;
}
