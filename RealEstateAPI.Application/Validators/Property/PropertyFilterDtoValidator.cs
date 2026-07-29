using FluentValidation;
using RealEstateAPI.Application.DTOs.Property;

namespace RealEstateAPI.Application.Validators.Property
{
    public class PropertyFilterDtoValidator : AbstractValidator<PropertyFilterDto>
    {
        private static readonly string[] AllowedSortFields =
            { "price", "area", "createdat", "bedrooms" };

        public PropertyFilterDtoValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

            RuleFor(x => x.MaxPrice)
                .GreaterThanOrEqualTo(x => x.MinPrice!.Value)
                .When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue)
                .WithMessage("MaxPrice must be greater than or equal to MinPrice");

            RuleFor(x => x.MaxArea)
                .GreaterThanOrEqualTo(x => x.MinArea!.Value)
                .When(x => x.MinArea.HasValue && x.MaxArea.HasValue)
                .WithMessage("MaxArea must be greater than or equal to MinArea");

            RuleFor(x => x.MaxBedrooms)
                .GreaterThanOrEqualTo(x => x.MinBedrooms!.Value)
                .When(x => x.MinBedrooms.HasValue && x.MaxBedrooms.HasValue)
                .WithMessage("MaxBedrooms must be greater than or equal to MinBedrooms");

            RuleFor(x => x.SortBy)
                .Must(v => AllowedSortFields.Contains(v!.ToLower()))
                .When(x => !string.IsNullOrWhiteSpace(x.SortBy))
                .WithMessage($"SortBy must be one of: {string.Join(", ", AllowedSortFields)}");

            RuleFor(x => x.SortOrder)
                .Must(v => v!.ToLower() is "asc" or "desc")
                .When(x => !string.IsNullOrWhiteSpace(x.SortOrder))
                .WithMessage("SortOrder must be 'asc' or 'desc'");
        }
    }
}
