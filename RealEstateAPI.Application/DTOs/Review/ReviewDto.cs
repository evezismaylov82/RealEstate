using System;

namespace RealEstateAPI.Application.DTOs.Review
{
    public class ReviewDto
    {
        public int Id { get; set; }
        public int ReviewerUserId { get; set; }
        public string ReviewerFullName { get; set; } = string.Empty;
        public int? PropertyId { get; set; }
        public string? PropertyTitle { get; set; }
        public int? RevieweeUserId { get; set; }
        public string? RevieweeFullName { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
