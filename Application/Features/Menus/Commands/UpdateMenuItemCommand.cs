using Application.Common;
using Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Menus.Commands
{
    public record UpdateMenuItemCommand(Guid MenuItemId, int DayOfWeek, MenuItemType Type, string Title, string? Description) : IRequest<Result>;
}
