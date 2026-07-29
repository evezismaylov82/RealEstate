using System.ComponentModel.DataAnnotations;

namespace RealEstateAPI.Application.DTOs.Role
{
    public class RoleUpdateDto
    {
        [StringLength(100, MinimumLength = 2)]
        public string? Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }
    }
}
