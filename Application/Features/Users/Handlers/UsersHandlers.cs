using Application.Common.Interfaces;
using Application.Common;
using Application.Features.Users.Commands;
using Application.Features.Users.Queries;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Users.Handlers
{
    public class CreateUserValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserValidator()
        {
            RuleFor(x => x.SchoolId).NotEmpty();
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ClassGroup).MaximumLength(100);
            RuleFor(x => x.Role).NotEmpty();
        }
    }

    public class CreateUserHandler : IRequestHandler<CreateUserCommand, Result<Guid>>
    {
        private readonly IApplicationDBContext _db;
        public CreateUserHandler(IApplicationDBContext db) => _db = db;

        public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var schoolExists = await _db.Schools.AnyAsync(s => s.SchoolId == request.SchoolId, cancellationToken);
            if (!schoolExists) return Result<Guid>.Failure(Error.NotFound("School not found."));

            var user = new User
            {
                UserId = Guid.NewGuid(),
                SchoolId = request.SchoolId,
                DisplayName = request.DisplayName,
                ClassGroup = request.ClassGroup ?? string.Empty,
                DefaultVegetarian = request.DefaultVegetarian,
                Role = Enum.TryParse<Role>(request.Role, true, out var r) ? r : Role.User,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Success(user.UserId);
        }
    }

    public class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
    {
        public UpdateUserValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ClassGroup).MaximumLength(100);
            RuleFor(x => x.Role).NotEmpty();
        }
    }

    public class UpdateUserHandler : IRequestHandler<UpdateUserCommand, Result>
    {
        private readonly IApplicationDBContext _db;
        public UpdateUserHandler(IApplicationDBContext db) => _db = db;

        public async Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken);
            if (user == null) return Result.Failure(Error.NotFound("User not found."));

            user.DisplayName = request.DisplayName;
            user.ClassGroup = request.ClassGroup ?? string.Empty;
            user.DefaultVegetarian = request.DefaultVegetarian;
            if (Enum.TryParse<Role>(request.Role, true, out var parsedRole)) user.Role = parsedRole;

            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }

    public class DeleteUserHandler : IRequestHandler<DeleteUserCommand, Result>
    {
        private readonly IApplicationDBContext _db;
        public DeleteUserHandler(IApplicationDBContext db) => _db = db;

        public async Task<Result> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken);
            if (user == null) return Result.Failure(Error.NotFound("User not found."));

            _db.Users.Remove(user);
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }

    public class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
    {
        private readonly IApplicationDBContext _db;
        public GetUserByIdHandler(IApplicationDBContext db) => _db = db;

        public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var user = await _db.Users
                .Include(u => u.UserAllergies)
                    .ThenInclude(ua => ua.Allergy)
                .FirstOrDefaultAsync(u => u.UserId == request.Id, cancellationToken);

            if (user == null) return Result<UserDto>.Failure(Error.NotFound("User not found."));

            var dto = new UserDto(
                user.UserId,
                user.SchoolId,
                user.DisplayName,
                user.ClassGroup,
                user.DefaultVegetarian,
                user.Role.ToString(),
                user.CreatedAt,
                user.UserAllergies.Select(ua => new UserAllergyDto(ua.AllergyId, ua.Allergy?.Name ?? string.Empty, ua.Notes))
            );

            return Result<UserDto>.Success(dto);
        }
    }

    public class ListUsersBySchoolHandler : IRequestHandler<ListUsersBySchoolQuery, Result<System.Collections.Generic.IEnumerable<UserDto>>>
    {
        private readonly IApplicationDBContext _db;
        public ListUsersBySchoolHandler(IApplicationDBContext db) => _db = db;

        public async Task<Result<System.Collections.Generic.IEnumerable<UserDto>>> Handle(ListUsersBySchoolQuery request, CancellationToken cancellationToken)
        {
            var users = await _db.Users
                .Where(u => u.SchoolId == request.SchoolId)
                .Include(u => u.UserAllergies)
                    .ThenInclude(ua => ua.Allergy)
                .ToListAsync(cancellationToken);

            var dtos = users.Select(u => new UserDto(
                u.UserId,
                u.SchoolId,
                u.DisplayName,
                u.ClassGroup,
                u.DefaultVegetarian,
                u.Role.ToString(),
                u.CreatedAt,
                u.UserAllergies.Select(ua => new UserAllergyDto(ua.AllergyId, ua.Allergy?.Name ?? string.Empty, ua.Notes))
            ));

            return Result<System.Collections.Generic.IEnumerable<UserDto>>.Success(dtos);
        }
    }
}
