using Application.Common.Interfaces;
using Application.Common;
using Application.Features.Menus.Commands;
using Application.Features.Menus.Queries;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Menus.Handlers
{
    public class CreateMenuWeekValidator : AbstractValidator<CreateMenuWeekCommand>
    {
        public CreateMenuWeekValidator()
        {
            RuleFor(x => x.SchoolId).NotEmpty();
            RuleFor(x => x.Year).InclusiveBetween(2000, 3000);
            RuleFor(x => x.WeekNumber).InclusiveBetween(1, 53);
        }
    }

    public class CreateMenuWeekHandler : IRequestHandler<CreateMenuWeekCommand, Result<Guid>>
    {
        private readonly IApplicationDBContext _db;
        public CreateMenuWeekHandler(IApplicationDBContext db) => _db = db;

        public async Task<Result<Guid>> Handle(CreateMenuWeekCommand request, CancellationToken cancellationToken)
        {
            var schoolExists = await _db.Schools.AnyAsync(s => s.SchoolId == request.SchoolId, cancellationToken);
            if (!schoolExists) return Result<Guid>.Failure(Error.NotFound("School not found."));

            var exists = await _db.MenuWeeks.AnyAsync(mw => mw.SchoolId == request.SchoolId && mw.Year == request.Year && mw.WeekNumber == request.WeekNumber, cancellationToken);
            if (exists) return Result<Guid>.Failure(Error.Conflict("MenuWeek already exists."));

            var mw = new MenuWeek { MenuWeekId = Guid.NewGuid(), SchoolId = request.SchoolId, Year = request.Year, WeekNumber = request.WeekNumber, PublishedAt = null };
            _db.MenuWeeks.Add(mw);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Success(mw.MenuWeekId);
        }
    }

    public class PublishMenuWeekHandler : IRequestHandler<PublishMenuWeekCommand, Result>
    {
        private readonly IApplicationDBContext _db;
        public PublishMenuWeekHandler(IApplicationDBContext db) => _db = db;

        public async Task<Result> Handle(PublishMenuWeekCommand request, CancellationToken cancellationToken)
        {
            var mw = await _db.MenuWeeks.FirstOrDefaultAsync(m => m.MenuWeekId == request.MenuWeekId, cancellationToken);
            if (mw == null) return Result.Failure(Error.NotFound("MenuWeek not found."));

            mw.PublishedAt ??= DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }

    public class GetMenuWeekHandler : IRequestHandler<GetMenuWeekQuery, Result<MenuWeekDto>>
    {
        private readonly IApplicationDBContext _db;
        public GetMenuWeekHandler(IApplicationDBContext db) => _db = db;

        public async Task<Result<MenuWeekDto>> Handle(GetMenuWeekQuery request, CancellationToken cancellationToken)
        {
            var dto = await _db.MenuWeeks
                .Where(mw => mw.SchoolId == request.SchoolId && mw.Year == request.Year && mw.WeekNumber == request.WeekNumber)
                .Select(mw => new MenuWeekDto(
                    mw.MenuWeekId,
                    mw.SchoolId,
                    mw.Year,
                    mw.WeekNumber,
                    mw.PublishedAt,
                    mw.MenuItems.Select(mi => new MenuItemDto(mi.MenuItemId, mi.DayOfWeek, mi.Type, mi.Title, mi.Description, mi.Allergens.Select(a => a.AllergenCode)))
                ))
                .FirstOrDefaultAsync(cancellationToken);

            if (dto == null) return Result<MenuWeekDto>.Failure(Error.NotFound("MenuWeek not found."));
            return Result<MenuWeekDto>.Success(dto);
        }
    }

    public class CreateMenuItemValidator : AbstractValidator<CreateMenuItemCommand>
    {
        public CreateMenuItemValidator()
        {
            RuleFor(x => x.MenuWeekId).NotEmpty();
            RuleFor(x => x.DayOfWeek).InclusiveBetween(1, 7);
            RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        }
    }

    public class CreateMenuItemHandler : IRequestHandler<CreateMenuItemCommand, Result<Guid>>
    {
        private readonly IApplicationDBContext _db;
        public CreateMenuItemHandler(IApplicationDBContext db) => _db = db;

        public async Task<Result<Guid>> Handle(CreateMenuItemCommand request, CancellationToken cancellationToken)
        {
            var mw = await _db.MenuWeeks.FindAsync(new object?[] { request.MenuWeekId }, cancellationToken);
            if (mw == null) return Result<Guid>.Failure(Error.NotFound("MenuWeek not found."));

            var mi = new MenuItem
            {
                MenuItemId = Guid.NewGuid(),
                MenuWeekId = mw.MenuWeekId,
                DayOfWeek = request.DayOfWeek,
                Type = request.Type,
                Title = request.Title,
                Description = request.Description ?? string.Empty
            };

            _db.MenuItems.Add(mi);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Success(mi.MenuItemId);
        }
    }

    public class UpdateMenuItemValidator : AbstractValidator<UpdateMenuItemCommand>
    {
        public UpdateMenuItemValidator()
        {
            RuleFor(x => x.MenuItemId).NotEmpty();
            RuleFor(x => x.DayOfWeek).InclusiveBetween(1, 7);
            RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        }
    }

    public class UpdateMenuItemHandler : IRequestHandler<UpdateMenuItemCommand, Result>
    {
        private readonly IApplicationDBContext _db;
        public UpdateMenuItemHandler(IApplicationDBContext db) => _db = db;

        public async Task<Result> Handle(UpdateMenuItemCommand request, CancellationToken cancellationToken)
        {
            var mi = await _db.MenuItems.FirstOrDefaultAsync(x => x.MenuItemId == request.MenuItemId, cancellationToken);
            if (mi == null) return Result.Failure(Error.NotFound("MenuItem not found."));

            mi.DayOfWeek = request.DayOfWeek;
            mi.Type = request.Type;
            mi.Title = request.Title;
            mi.Description = request.Description ?? string.Empty;

            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }

    public class RemoveMenuItemHandler : IRequestHandler<RemoveMenuItemCommand, Result>
    {
        private readonly IApplicationDBContext _db;
        public RemoveMenuItemHandler(IApplicationDBContext db) => _db = db;

        public async Task<Result> Handle(RemoveMenuItemCommand request, CancellationToken cancellationToken)
        {
            var mi = await _db.MenuItems.FirstOrDefaultAsync(x => x.MenuItemId == request.MenuItemId, cancellationToken);
            if (mi == null) return Result.Failure(Error.NotFound("MenuItem not found."));

            _db.MenuItems.Remove(mi);
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }

    public class SetMenuItemAllergensValidator : AbstractValidator<SetMenuItemAllergensCommand>
    {
        public SetMenuItemAllergensValidator() => RuleFor(x => x.MenuItemId).NotEmpty();
    }

    public class SetMenuItemAllergensHandler : IRequestHandler<SetMenuItemAllergensCommand, Result>
    {
        private readonly IApplicationDBContext _db;
        public SetMenuItemAllergensHandler(IApplicationDBContext db) => _db = db;

        public async Task<Result> Handle(SetMenuItemAllergensCommand request, CancellationToken cancellationToken)
        {
            var mi = await _db.MenuItems.Include(m => m.Allergens).FirstOrDefaultAsync(x => x.MenuItemId == request.MenuItemId, cancellationToken);
            if (mi == null) return Result.Failure(Error.NotFound("MenuItem not found."));

            // Remove existing
            var existing = mi.Allergens.ToList();
            foreach (var ex in existing) _db.MenuItemAllergens.Remove(ex);

            // Normalize + distinct
            var codes = request.AllergenCodes
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim().ToUpperInvariant())
                .Distinct();

            foreach (var code in codes)
            {
                _db.MenuItemAllergens.Add(new MenuItemAllergen { MenuItemId = mi.MenuItemId, AllergenCode = code });
            }

            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }

    public class ListMenuItemsForWeekHandler : IRequestHandler<ListMenuItemsForWeekQuery, Result<IEnumerable<MenuItemDto>>>
    {
        private readonly IApplicationDBContext _db;
        public ListMenuItemsForWeekHandler(IApplicationDBContext db) => _db = db;

        public async Task<Result<IEnumerable<MenuItemDto>>> Handle(ListMenuItemsForWeekQuery request, CancellationToken cancellationToken)
        {
            var mwExists = await _db.MenuWeeks.AnyAsync(m => m.MenuWeekId == request.MenuWeekId, cancellationToken);
            if (!mwExists) return Result<IEnumerable<MenuItemDto>>.Failure(Error.NotFound("MenuWeek not found."));

            var items = await _db.MenuItems
                .Where(mi => mi.MenuWeekId == request.MenuWeekId)
                .Include(mi => mi.Allergens)
                .OrderBy(mi => mi.DayOfWeek)
                .ThenBy(mi => mi.Type)
                .Select(mi => new MenuItemDto(mi.MenuItemId, mi.DayOfWeek, mi.Type, mi.Title, mi.Description, mi.Allergens.Select(a => a.AllergenCode)))
                .ToListAsync(cancellationToken);

            return Result<IEnumerable<MenuItemDto>>.Success(items);
        }
    }
}

