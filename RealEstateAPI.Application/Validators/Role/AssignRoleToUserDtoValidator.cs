using FluentValidation;
using RealEstateAPI.Application.DTOs.Role;

namespace RealEstateAPI.Application.Validators.Role
{
    public class AssignRoleToUserDtoValidator : AbstractValidator<AssignRoleToUserDto>
    {
        public AssignRoleToUserDtoValidator()
        {
            RuleFor(x => x.RoleId).GreaterThan(0);
        }
    }
}
