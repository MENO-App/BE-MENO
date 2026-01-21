using Application.Common;
using Domain.Enums;
using MediatR;

namespace Application.Features.MealPlans.Commands
{
    public record SetMealPlanCommand(Guid UserId, DateOnly Date, MealChoiceStatus Status, bool WantsVegetarian) : IRequest<Result>;
}
