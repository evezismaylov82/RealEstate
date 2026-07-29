using FluentValidation;
using RealEstateAPI.Application.DTOs.Booking;

namespace RealEstateAPI.Application.Validators.Booking
{
    public class BookingUpdateStatusDtoValidator : AbstractValidator<BookingUpdateStatusDto>
    {
        public BookingUpdateStatusDtoValidator()
        {
            RuleFor(x => x.Status).IsInEnum();
            RuleFor(x => x.CancellationReason).MaximumLength(500);
        }
    }
}
