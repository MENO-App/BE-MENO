using Application.Common;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Menus.Commands
{
    public record PublishMenuWeekCommand(Guid MenuWeekId) : IRequest<Result>;
}
