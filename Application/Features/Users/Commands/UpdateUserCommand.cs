using Application.Common;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Users.Commands
{
    public record UpdateUserCommand(Guid UserId, string DisplayName, string ClassGroup, bool DefaultVegetarian, string Role) : IRequest<Result>;
}
