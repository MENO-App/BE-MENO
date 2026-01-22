using Application.Common;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Menus.Queries
{
    public record ListMenuItemsForWeekQuery(Guid MenuWeekId) : IRequest<Result<IEnumerable<MenuItemDto>>>;
}
