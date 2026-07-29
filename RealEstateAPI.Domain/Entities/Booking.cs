using RealEstateAPI.Domain.Entities.Base;
using RealEstateAPI.Domain.Enums;
using System;

namespace RealEstateAPI.Domain.Entities
{

    public class Booking : BaseEntity
    {
        public int PropertyId { get; set; }
        public virtual Property Property { get; set; }

        public int UserId { get; set; }
        public virtual User User { get; set; }

        public DateTime RequestedDateTime { get; set; }
        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        public string? Notes { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }

        public string? CancellationReason { get; set; }
        public DateTime? RespondedAt { get; set; }
    }
}
