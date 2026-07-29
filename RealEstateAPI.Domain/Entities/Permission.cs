using RealEstateAPI.Domain.Entities.Base;
using System.Collections.Generic;

namespace RealEstateAPI.Domain.Entities
{
    public class Permission : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
