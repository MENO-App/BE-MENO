using Application.Common;
using MediatR;

namespace Application.Features.MealPlans.Queries
{
    public record ListMealPlansForUserRangeQuery(Guid UserId, DateOnly From, DateOnly To) : IRequest<Result<IEnumerable<MealPlanDto>>>;
}
