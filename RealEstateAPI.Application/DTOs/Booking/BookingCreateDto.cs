using System;
using System.ComponentModel.DataAnnotations;

namespace RealEstateAPI.Application.DTOs.Booking
{
    public class BookingCreateDto
    {
        [Required(ErrorMessage = "Property is required")]
        public int PropertyId { get; set; }

        [Required(ErrorMessage = "Requested date and time is required")]
        public DateTime RequestedDateTime { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(20)]
        public string? ContactPhone { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(255)]
        public string? ContactEmail { get; set; }
    }
}
