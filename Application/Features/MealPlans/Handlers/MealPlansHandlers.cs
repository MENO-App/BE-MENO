using Application.Common.Interfaces;
using Application.Common;
using Application.Features.MealPlans.Commands;
using Application.Features.MealPlans.Queries;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.MealPlans.Handlers
{
    public class SetMealPlanValidator : AbstractValidator<SetMealPlanCommand>
    {
        public SetMealPlanValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Date).Must(d => d.Year >= 2000).WithMessage("Invalid date");
        }
    }

    public class SetMealPlanHandler : IRequestHandler<SetMealPlanCommand, Result>
    {
        private readonly IApplicationDBContext _db;
        public SetMealPlanHandler(IApplicationDBContext db) => _db = db;

        public async Task<Result> Handle(SetMealPlanCommand request, CancellationToken cancellationToken)
        {
            var userExists = await _db.Users.AnyAsync(u => u.UserId == request.UserId, cancellationToken);
            if (!userExists) return Result.Failure(Error.NotFound("User not found."));

            var existing = await _db.MealPlans.FindAsync(new object?[] { request.UserId, request.Date }, cancellationToken);

            if (existing == null)
            {
                var mp = new MealPlan
                {
                    UserId = request.UserId,
                    Date = request.Date,
                    Status = request.Status,
                    WantsVegetarian = request.WantsVegetarian
                };
                _db.MealPlans.Add(mp);
            }
            else
            {
                existing.Status = request.Status;
                existing.WantsVegetarian = request.WantsVegetarian;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }

    public class GetMealPlanForDateHandler : IRequestHandler<GetMealPlanForDateQuery, Result<MealPlanDto>>
    {
        private readonly IApplicationDBContext _db;
        public GetMealPlanForDateHandler(IApplicationDBContext db) => _db = db;

        public async Task<Result<MealPlanDto>> Handle(GetMealPlanForDateQuery request, CancellationToken cancellationToken)
        {
            var mp = await _db.MealPlans.FirstOrDefaultAsync(x => x.UserId == request.UserId && x.Date == request.Date, cancellationToken);
            if (mp == null) return Result<MealPlanDto>.Failure(Error.NotFound("MealPlan not found."));

            var dto = new MealPlanDto(mp.UserId, mp.Date, mp.Status, mp.WantsVegetarian);
            return Result<MealPlanDto>.Success(dto);
        }
    }

    public class ListMealPlansForUserRangeValidator : AbstractValidator<ListMealPlansForUserRangeQuery>
    {
        public ListMealPlansForUserRangeValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.From).Must(d => d.Year >= 2000);
            RuleFor(x => x.To).Must(d => d.Year >= 2000);
            RuleFor(x => x).Must(x => x.To >= x.From).WithMessage("'To' must be >= 'From'");
        }
    }

    public class ListMealPlansForUserRangeHandler : IRequestHandler<ListMealPlansForUserRangeQuery, Result<IEnumerable<MealPlanDto>>>
    {
        private readonly IApplicationDBContext _db;
        public ListMealPlansForUserRangeHandler(IApplicationDBContext db) => _db = db;

        public async Task<Result<IEnumerable<MealPlanDto>>> Handle(ListMealPlansForUserRangeQuery request, CancellationToken cancellationToken)
        {
            var userExists = await _db.Users.AnyAsync(u => u.UserId == request.UserId, cancellationToken);
            if (!userExists) return Result<IEnumerable<MealPlanDto>>.Failure(Error.NotFound("User not found."));

            var items = await _db.MealPlans
                .Where(mp => mp.UserId == request.UserId && mp.Date >= request.From && mp.Date <= request.To)
                .OrderBy(mp => mp.Date)
                .Select(mp => new MealPlanDto(mp.UserId, mp.Date, mp.Status, mp.WantsVegetarian))
                .ToListAsync(cancellationToken);

            return Result<IEnumerable<MealPlanDto>>.Success(items);
        }
    }
}

