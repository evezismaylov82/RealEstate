using FluentValidation;
using RealEstateAPI.Application.DTOs.Category;

namespace RealEstateAPI.Application.Validators.Category
{
    public class CategoryCreateDtoValidator : AbstractValidator<CategoryCreateDto>
    {
        public CategoryCreateDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty().Length(2, 100);
            RuleFor(x => x.Description).MaximumLength(500);
            RuleFor(x => x.IconUrl).MaximumLength(500);
            RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ParentCategoryId).GreaterThan(0).When(x => x.ParentCategoryId.HasValue);
        }
    }
}
