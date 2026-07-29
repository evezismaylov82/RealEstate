using System;

namespace RealEstateAPI.Application.DTOs.Booking
{
    public class BookingDto
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public string PropertyTitle { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public DateTime RequestedDateTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime? RespondedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
