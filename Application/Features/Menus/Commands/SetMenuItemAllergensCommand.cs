using Application.Common;
using MediatR;

namespace Application.Features.Menus.Commands
{
    public record SetMenuItemAllergensCommand(Guid MenuItemId, IEnumerable<string> AllergenCodes) : IRequest<Result>;
}
