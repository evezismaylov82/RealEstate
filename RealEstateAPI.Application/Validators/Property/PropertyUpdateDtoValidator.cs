using FluentValidation;
using RealEstateAPI.Application.DTOs.Property;

namespace RealEstateAPI.Application.Validators.Property
{
    public class PropertyUpdateDtoValidator : AbstractValidator<PropertyUpdateDto>
    {
        public PropertyUpdateDtoValidator()
        {
            RuleFor(x => x.Title).Length(10, 200).When(x => x.Title != null);
            RuleFor(x => x.Description).Length(50, 5000).When(x => x.Description != null);
            RuleFor(x => x.Type).IsInEnum().When(x => x.Type.HasValue);
            RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
            RuleFor(x => x.Price).GreaterThan(0).When(x => x.Price.HasValue);
            RuleFor(x => x.Currency).Length(3).When(x => x.Currency != null);
            RuleFor(x => x.Area).InclusiveBetween(1, 100000).When(x => x.Area.HasValue);
            RuleFor(x => x.Bedrooms).InclusiveBetween(0, 50).When(x => x.Bedrooms.HasValue);
            RuleFor(x => x.Bathrooms).InclusiveBetween(0, 50).When(x => x.Bathrooms.HasValue);
            RuleFor(x => x.LivingRooms).InclusiveBetween(0, 50).When(x => x.LivingRooms.HasValue);
            RuleFor(x => x.Floor).InclusiveBetween(-5, 200).When(x => x.Floor.HasValue);
            RuleFor(x => x.TotalFloors).InclusiveBetween(1, 200).When(x => x.TotalFloors.HasValue);
            RuleFor(x => x.BuildYear).InclusiveBetween(1800, 2100).When(x => x.BuildYear.HasValue);
            RuleFor(x => x.Country).MaximumLength(100).When(x => x.Country != null);
            RuleFor(x => x.City).MaximumLength(100).When(x => x.City != null);
            RuleFor(x => x.District).MaximumLength(100).When(x => x.District != null);
            RuleFor(x => x.Address).MaximumLength(500).When(x => x.Address != null);
            RuleFor(x => x.VideoUrl).Must(BeAValidUrl).When(x => !string.IsNullOrWhiteSpace(x.VideoUrl))
                .WithMessage("Invalid video URL");
            RuleFor(x => x.VirtualTourUrl).Must(BeAValidUrl).When(x => !string.IsNullOrWhiteSpace(x.VirtualTourUrl))
                .WithMessage("Invalid virtual tour URL");
        }

        private static bool BeAValidUrl(string? url) =>
            Uri.TryCreate(url, UriKind.Absolute, out _);
    }
}
