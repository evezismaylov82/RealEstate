using FluentValidation;
using RealEstateAPI.Application.DTOs.Review;

namespace RealEstateAPI.Application.Validators.Review
{
    public class ReviewCreateDtoValidator : AbstractValidator<ReviewCreateDto>
    {
        public ReviewCreateDtoValidator()
        {
            RuleFor(x => x.Rating).InclusiveBetween(1, 5);
            RuleFor(x => x.Comment).NotEmpty().Length(3, 2000);

            RuleFor(x => x)
                .Must(x => x.PropertyId.HasValue ^ x.RevieweeUserId.HasValue)
                .WithMessage("A review must target exactly one of: property, user")
                .WithName("Target");

            RuleFor(x => x.PropertyId).GreaterThan(0).When(x => x.PropertyId.HasValue);
            RuleFor(x => x.RevieweeUserId).GreaterThan(0).When(x => x.RevieweeUserId.HasValue);
        }
    }
}
