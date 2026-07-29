using System.ComponentModel.DataAnnotations;

namespace RealEstateAPI.Application.DTOs.Category
{
    public class CategoryCreateDto
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? IconUrl { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public int? ParentCategoryId { get; set; }
    }
}
