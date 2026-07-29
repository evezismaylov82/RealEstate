using FluentValidation;
using RealEstateAPI.Application.DTOs.Auth;

namespace RealEstateAPI.Application.Validators.Auth
{
    public class UpdateProfileDtoValidator : AbstractValidator<UpdateProfileDto>
    {
        public UpdateProfileDtoValidator()
        {
            RuleFor(x => x.FirstName).Length(2, 100).When(x => x.FirstName != null);
            RuleFor(x => x.LastName).Length(2, 100).When(x => x.LastName != null);
            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\+?[0-9\s\-()]{7,20}$")
                .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
                .WithMessage("Invalid phone number");
            RuleFor(x => x.ProfileImageUrl).MaximumLength(500);
        }
    }
}
