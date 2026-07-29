using RealEstateAPI.Domain.Entities.Base;

namespace RealEstateAPI.Domain.Entities
{

    public class Review : BaseEntity
    {
        public int ReviewerUserId { get; set; }
        public virtual User ReviewerUser { get; set; }

        public int? PropertyId { get; set; }
        public virtual Property? Property { get; set; }

        public int? RevieweeUserId { get; set; }
        public virtual User? RevieweeUser { get; set; }

        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;

        public bool IsApproved { get; set; } = true;
    }
}
