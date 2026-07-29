using FluentValidation;
using RealEstateAPI.Application.DTOs.Role;

namespace RealEstateAPI.Application.Validators.Role
{
    public class RoleCreateDtoValidator : AbstractValidator<RoleCreateDto>
    {
        public RoleCreateDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty().Length(2, 100);
            RuleFor(x => x.Description).MaximumLength(500);
        }
    }
}
