using Application.Common.Constants;
using Application.Users.Allergies.Dtos;
using FluentValidation;

namespace Application.Users.Allergies.Validators;

public sealed class AddUserAllergyRequestValidator : AbstractValidator<AddUserAllergyRequest>
{
    public AddUserAllergyRequestValidator()
    {
        RuleFor(x => x.AllergyId)
            .NotEmpty();

        //if user picks "Annan" must Notes be filled
        When(x => x.AllergyId == AllergyConstants.OtherAllergyId, () =>
        {
            RuleFor(x => x.Notes)
                .NotEmpty().WithMessage("Notes is required when selecting 'Annan'.")
                .MinimumLength(2).WithMessage("Notes must be at least 2 characters.")
                .MaximumLength(100).WithMessage("Notes must be at most 100 characters.");
        });

        // if not "annan" , Notes can be max 100 chars
        When(x => x.AllergyId != AllergyConstants.OtherAllergyId, () =>
        {
            RuleFor(x => x.Notes)
                .MaximumLength(100);
        });
    }
}
