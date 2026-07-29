using RealEstateAPI.Domain.Entities.Base;
using RealEstateAPI.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateAPI.Domain.Entities
{
    public  class Payment : BaseEntity
    {
        public int? UserId { get; set; }
        public virtual User User { get; set; }
        public int? PropertyId { get; set; }
        public virtual Property Property { get; set;  }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public PaymentType Type { get; set; }
        public PaymentStatus Status { get; set; }
        public string PaymentMethod { get; set;  }
        public string TransactionId { get; set; }
        public string? Description { get; set;  }
        public DateTime? PaidAt { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
