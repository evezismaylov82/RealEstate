using System.ComponentModel.DataAnnotations;

namespace RealEstateAPI.Application.DTOs.Role
{
    public class RoleCreateDto
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
    }
}
