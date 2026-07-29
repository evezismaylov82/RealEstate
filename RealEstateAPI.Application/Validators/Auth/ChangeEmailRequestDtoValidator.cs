using FluentValidation;
using RealEstateAPI.Application.DTOs.Auth;

namespace RealEstateAPI.Application.Validators.Auth
{
    public class ChangeEmailRequestDtoValidator : AbstractValidator<ChangeEmailRequestDto>
    {
        public ChangeEmailRequestDtoValidator()
        {
            RuleFor(x => x.NewEmail).NotEmpty().EmailAddress().MaximumLength(255);
            RuleFor(x => x.CurrentPassword).NotEmpty();
        }
    }
}
