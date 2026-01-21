using Application.Common.Interfaces;
using Application.Common;
using Application.Features.Allergies.Commands;
using Application.Features.Allergies.Queries;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Allergies.Handlers
{
    public class CreateAllergyValidator : AbstractValidator<CreateAllergyCommand>
    {
        public CreateAllergyValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }

    public class CreateAllergyHandler : IRequestHandler<CreateAllergyCommand, Result<Guid>>
    {
        private readonly IApplicationDBContext _db;
        public CreateAllergyHandler(IApplicationDBContext db) => _db = db;

        public async Task<Result<Guid>> Handle(CreateAllergyCommand request, CancellationToken cancellationToken)
        {
            var exists = await _db.Allergies.AnyAsync(a => a.Name == request.Name, cancellationToken);
            if (exists) return Result<Guid>.Failure(Error.Conflict("Allergy already exists."));

            var entity = new Allergy { AllergyId = Guid.NewGuid(), Name = request.Name };
            _db.Allergies.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Success(entity.AllergyId);
        }
    }

    public class ListAllergiesHandler : IRequestHandler<ListAllergiesQuery, Result<IEnumerable<AllergyDto>>>
    {
        private readonly IApplicationDBContext _db;
        public ListAllergiesHandler(IApplicationDBContext db) => _db = db;

        public async Task<Result<IEnumerable<AllergyDto>>> Handle(ListAllergiesQuery request, CancellationToken cancellationToken)
        {
            var items = await _db.Allergies.OrderBy(a => a.Name)
                .Select(a => new AllergyDto(a.AllergyId, a.Name))
                .ToListAsync(cancellationToken);
            return Result<IEnumerable<AllergyDto>>.Success(items);
        }
    }

    public class AddUserAllergyValidator : AbstractValidator<AddUserAllergyCommand>
    {
        public AddUserAllergyValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.AllergyId).NotEmpty();
            RuleFor(x => x.Notes).MaximumLength(1000);
        }
    }

    public class AddUserAllergyHandler : IRequestHandler<AddUserAllergyCommand, Result>
    {
        private readonly IApplicationDBContext _db;
        public AddUserAllergyHandler(IApplicationDBContext db) => _db = db;

        public async Task<Result> Handle(AddUserAllergyCommand request, CancellationToken cancellationToken)
        {
            var user = await _db.Users.FindAsync(new object?[] { request.UserId }, cancellationToken);
            if (user == null) return Result.Failure(Error.NotFound("User not found."));

            var allergy = await _db.Allergies.FindAsync(new object?[] { request.AllergyId }, cancellationToken);
            if (allergy == null) return Result.Failure(Error.NotFound("Allergy not found."));

            var exists = await _db.UserAllergies.AnyAsync(ua => ua.UserId == request.UserId && ua.AllergyId == request.AllergyId, cancellationToken);
            if (exists) return Result.Failure(Error.Conflict("User already has this allergy."));

            _db.UserAllergies.Add(new UserAllergy { UserId = request.UserId, AllergyId = request.AllergyId, Notes = request.Notes ?? string.Empty });
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }

    public class RemoveUserAllergyHandler : IRequestHandler<RemoveUserAllergyCommand, Result>
    {
        private readonly IApplicationDBContext _db;
        public RemoveUserAllergyHandler(IApplicationDBContext db) => _db = db;

        public async Task<Result> Handle(RemoveUserAllergyCommand request, CancellationToken cancellationToken)
        {
            var ua = await _db.UserAllergies.FirstOrDefaultAsync(x => x.UserId == request.UserId && x.AllergyId == request.AllergyId, cancellationToken);
            if (ua == null) return Result.Failure(Error.NotFound("UserAllergy not found."));

            _db.UserAllergies.Remove(ua);
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }

    public class ListUserAllergiesHandler : IRequestHandler<ListUserAllergiesQuery, Result<IEnumerable<UserAllergyDto>>>
    {
        private readonly IApplicationDBContext _db;
        public ListUserAllergiesHandler(IApplicationDBContext db) => _db = db;

        public async Task<Result<IEnumerable<UserAllergyDto>>> Handle(ListUserAllergiesQuery request, CancellationToken cancellationToken)
        {
            var userExists = await _db.Users.AnyAsync(u => u.UserId == request.UserId, cancellationToken);
            if (!userExists) return Result<IEnumerable<UserAllergyDto>>.Failure(Error.NotFound("User not found."));

            var items = await _db.UserAllergies
                .Where(ua => ua.UserId == request.UserId)
                .Include(ua => ua.Allergy)
                .Select(ua => new UserAllergyDto(ua.AllergyId, ua.Allergy!.Name, ua.Notes))
                .ToListAsync(cancellationToken);

            return Result<IEnumerable<UserAllergyDto>>.Success(items);
        }
    }
}
