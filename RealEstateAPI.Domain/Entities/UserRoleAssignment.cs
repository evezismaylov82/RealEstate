using System;

namespace RealEstateAPI.Domain.Entities
{
    public class UserRoleAssignment
    {
        public int UserId { get; set; }
        public virtual User User { get; set; }

        public int RoleId { get; set; }
        public virtual Role Role { get; set; }

        public DateTime AssignedAt { get; set; }
    }
}
