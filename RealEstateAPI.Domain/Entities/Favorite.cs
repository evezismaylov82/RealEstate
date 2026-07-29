using RealEstateAPI.Domain.Entities.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateAPI.Domain.Entities
{
    public class Favorite : BaseEntity
    {
        public int? UserId { get; set;  }
        public virtual User User { get; set; }
        public int? PropertyId { get; set; }
        public virtual Property Property { get; set; }
        public string? Note { get; set; }
    }
}
