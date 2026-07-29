using RealEstateAPI.Domain.Entities.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateAPI.Domain.Entities
{
    public class ContactMessage : BaseEntity
    {
        public int? UserId { get; set; }
        public virtual User? User { get; set; }
        public int? PropertyId { get; set;  }
        public virtual Property? Property { get; set;  }
        public string Name { get; set;  }
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set;  }
        public DateTime? ReadAt { get; set; }
        public bool IsReplied { get; set;  }
        public string? ReplyMessage { get; set; }
        public DateTime? RepliedAt { get; set; }
        public int? RepliedByUserId { get; set;  }
    }
}
