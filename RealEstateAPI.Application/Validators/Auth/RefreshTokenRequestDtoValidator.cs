using FluentValidation;
using RealEstateAPI.Application.DTOs.Auth;

namespace RealEstateAPI.Application.Validators.Auth
{
    public class RefreshTokenRequestDtoValidator : AbstractValidator<RefreshTokenRequestDto>
    {
        public RefreshTokenRequestDtoValidator()
        {
            RuleFor(x => x.RefreshToken).NotEmpty();
        }
    }
}
