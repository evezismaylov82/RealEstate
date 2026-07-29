using System.ComponentModel.DataAnnotations;
using RealEstateAPI.Domain.Enums;

namespace RealEstateAPI.Application.DTOs.Booking
{
    public class BookingUpdateStatusDto
    {
        [Required(ErrorMessage = "Status is required")]
        public BookingStatus Status { get; set; }

        [StringLength(500)]
        public string? CancellationReason { get; set; }
    }
}
