using Application.Common;
using Domain.Enums;
using MediatR;

namespace Application.Features.MealPlans.Queries
{
    public record GetMealPlanForDateQuery(Guid UserId, DateOnly Date) : IRequest<Result<MealPlanDto>>;
    public record MealPlanDto(Guid UserId, DateOnly Date, MealChoiceStatus Status, bool WantsVegetarian);
}
