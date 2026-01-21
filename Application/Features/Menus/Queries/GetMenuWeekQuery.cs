using Application.Common;
using Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Menus.Queries
{
    public record GetMenuWeekQuery(Guid SchoolId, int Year, int WeekNumber) : IRequest<Result<MenuWeekDto>>;
    public record MenuWeekDto(Guid MenuWeekId, Guid SchoolId, int Year, int WeekNumber, DateTime? PublishedAt, IEnumerable<MenuItemDto> Items);
    public record MenuItemDto(Guid MenuItemId, int DayOfWeek, MenuItemType Type, string Title, string Description, IEnumerable<string> Allergens);
}
