using FluentValidation;
using RealEstateAPI.Application.DTOs.Role;

namespace RealEstateAPI.Application.Validators.Role
{
    public class RoleUpdateDtoValidator : AbstractValidator<RoleUpdateDto>
    {
        public RoleUpdateDtoValidator()
        {
            RuleFor(x => x.Name).Length(2, 100).When(x => x.Name != null);
            RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description != null);
        }
    }
}
