using FluentValidation;
using RealEstateAPI.Application.DTOs.Category;

namespace RealEstateAPI.Application.Validators.Category
{
    public class CategoryUpdateDtoValidator : AbstractValidator<CategoryUpdateDto>
    {
        public CategoryUpdateDtoValidator()
        {
            RuleFor(x => x.Name).Length(2, 100).When(x => x.Name != null);
            RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description != null);
            RuleFor(x => x.IconUrl).MaximumLength(500).When(x => x.IconUrl != null);
            RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0).When(x => x.DisplayOrder.HasValue);
            RuleFor(x => x.ParentCategoryId).GreaterThan(0).When(x => x.ParentCategoryId.HasValue);
        }
    }
}
