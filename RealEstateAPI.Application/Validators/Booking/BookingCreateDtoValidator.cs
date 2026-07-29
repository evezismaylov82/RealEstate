using FluentValidation;
using RealEstateAPI.Application.DTOs.Booking;

namespace RealEstateAPI.Application.Validators.Booking
{
    public class BookingCreateDtoValidator : AbstractValidator<BookingCreateDto>
    {
        public BookingCreateDtoValidator()
        {
            RuleFor(x => x.PropertyId).GreaterThan(0);

            RuleFor(x => x.RequestedDateTime)
                .GreaterThan(DateTime.UtcNow)
                .WithMessage("Requested date and time must be in the future")
                .LessThan(DateTime.UtcNow.AddMonths(6))
                .WithMessage("Bookings cannot be requested more than 6 months in advance");

            RuleFor(x => x.Notes).MaximumLength(1000);

            RuleFor(x => x.ContactPhone)
                .Matches(@"^\+?[0-9\s\-()]{7,20}$")
                .When(x => !string.IsNullOrWhiteSpace(x.ContactPhone))
                .WithMessage("Invalid phone number");

            RuleFor(x => x.ContactEmail)
                .EmailAddress()
                .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
        }
    }
}
