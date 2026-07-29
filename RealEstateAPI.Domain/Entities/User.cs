using RealEstateAPI.Domain.Entities.Base;
using RealEstateAPI.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateAPI.Domain.Entities
{
    public class User : BaseEntity
    {
        public string FirstName { get; set;  }
        public string LastName { get; set;  }
        public string Email { get; set; }
        public string? Username { get; set; }
        public string PasswordHash { get; set;  }
        public string? PhoneNumber { get; set;  }
        public string? ProfileImageUrl { get; set;  }
        public UserRole Role { get; set;  }
        public bool IsEmailVerified { get; set;  }
        public string? EmailVerificationToken { get; set;  }
        public string? PasswordResetToken { get; set;  }
        public DateTime? PasswordResetTokenExpiry { get; set;  }
        public DateTime? LastLoginAt { get; set; }

        public string? PendingEmail { get; set; }
        public string? EmailChangeToken { get; set; }
        public DateTime? EmailChangeTokenExpiry { get; set; }

        public bool IsLocked { get; set; }
        public DateTime? LockedUntil { get; set; }
        public string? LockReason { get; set; }

        public virtual ICollection<Property> Properties { get; set; }
        public virtual ICollection<Favorite> Favorites { get; set; }
        public virtual ICollection<Payment> Payments { get; set; }
        public virtual ICollection<ContactMessage> ContactMessages { get; set; }
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; }
        public virtual ICollection<UserRoleAssignment> UserRoleAssignments { get; set; }
        public virtual ICollection<Booking> Bookings { get; set; }
        public virtual ICollection<Review> ReviewsWritten { get; set; }
        public virtual ICollection<Review> ReviewsReceived { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public bool IsAdmin => Role == UserRole.Admin;
        public bool IsAgent => Role == UserRole.Agent;

        public bool IsCurrentlyLocked => IsLocked && (LockedUntil == null || LockedUntil > DateTime.UtcNow);
    }
}
