using FluentValidation;
using RealEstateAPI.Application.DTOs.Role;

namespace RealEstateAPI.Application.Validators.Role
{
    public class AssignPermissionsDtoValidator : AbstractValidator<AssignPermissionsDto>
    {
        public AssignPermissionsDtoValidator()
        {
            RuleFor(x => x.PermissionIds).NotNull();
            RuleForEach(x => x.PermissionIds).GreaterThan(0);
        }
    }
}
