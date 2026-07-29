using FluentValidation;
using RealEstateAPI.Application.DTOs.Auth;

namespace RealEstateAPI.Application.Validators.Auth
{
    public class ChangeUsernameDtoValidator : AbstractValidator<ChangeUsernameDto>
    {
        public ChangeUsernameDtoValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty()
                .Length(3, 30)
                .Matches(@"^[a-zA-Z0-9_.]+$")
                .WithMessage("Username can only contain letters, numbers, dots and underscores");
        }
    }
}
