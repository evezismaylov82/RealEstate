using FluentValidation;
using RealEstateAPI.Application.DTOs.Auth;

namespace RealEstateAPI.Application.Validators.Auth
{
    public class ConfirmEmailChangeDtoValidator : AbstractValidator<ConfirmEmailChangeDto>
    {
        public ConfirmEmailChangeDtoValidator()
        {
            RuleFor(x => x.Token).NotEmpty();
        }
    }
}
