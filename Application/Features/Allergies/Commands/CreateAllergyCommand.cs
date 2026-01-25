using Application.Common;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Allergies.Commands
{
    public record CreateAllergyCommand(string Name) : IRequest<Result<Guid>>;
}
