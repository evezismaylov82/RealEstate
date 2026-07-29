using FluentValidation;
using RealEstateAPI.Application.DTOs.Property;

namespace RealEstateAPI.Application.Validators.Property
{
    public class PropertyCreateDtoValidator : AbstractValidator<PropertyCreateDto>
    {
        public PropertyCreateDtoValidator()
        {
            RuleFor(x => x.Title).NotEmpty().Length(10, 200);
            RuleFor(x => x.Description).NotEmpty().Length(50, 5000);
            RuleFor(x => x.Type).IsInEnum();
            RuleFor(x => x.Status).IsInEnum();
            RuleFor(x => x.Price).GreaterThan(0);
            RuleFor(x => x.Currency).Length(3);
            RuleFor(x => x.Area).InclusiveBetween(1, 100000);
            RuleFor(x => x.Bedrooms).InclusiveBetween(0, 50);
            RuleFor(x => x.Bathrooms).InclusiveBetween(0, 50);
            RuleFor(x => x.LivingRooms).InclusiveBetween(0, 50);
            RuleFor(x => x.Floor).InclusiveBetween(-5, 200).When(x => x.Floor.HasValue);
            RuleFor(x => x.TotalFloors).InclusiveBetween(1, 200).When(x => x.TotalFloors.HasValue);
            RuleFor(x => x.BuildYear).InclusiveBetween(1800, 2100).When(x => x.BuildYear.HasValue);
            RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
            RuleFor(x => x.City).NotEmpty().MaximumLength(100);
            RuleFor(x => x.District).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
            RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
            RuleFor(x => x.VideoUrl).Must(BeAValidUrl).When(x => !string.IsNullOrWhiteSpace(x.VideoUrl))
                .WithMessage("Invalid video URL");
            RuleFor(x => x.VirtualTourUrl).Must(BeAValidUrl).When(x => !string.IsNullOrWhiteSpace(x.VirtualTourUrl))
                .WithMessage("Invalid virtual tour URL");
            RuleFor(x => x.Floor)
                .LessThanOrEqualTo(x => x.TotalFloors!.Value)
                .When(x => x.Floor.HasValue && x.TotalFloors.HasValue)
                .WithMessage("Floor cannot be greater than total floors");
        }

        private static bool BeAValidUrl(string? url) =>
            Uri.TryCreate(url, UriKind.Absolute, out _);
    }
}
