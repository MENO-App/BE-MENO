using Application.Common;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Users.Queries
{
    public record ListUsersBySchoolQuery(Guid SchoolId) : IRequest<Result<IEnumerable<UserDto>>>;
}
