using System.ComponentModel.DataAnnotations;

namespace RealEstateAPI.Application.DTOs.Review
{

    public class ReviewCreateDto
    {
        public int? PropertyId { get; set; }

        public int? RevieweeUserId { get; set; }

        [Required(ErrorMessage = "Rating is required")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Comment is required")]
        [StringLength(2000, MinimumLength = 3)]
        public string Comment { get; set; } = string.Empty;
    }
}
