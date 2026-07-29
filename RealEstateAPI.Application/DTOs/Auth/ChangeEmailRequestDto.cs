using System.ComponentModel.DataAnnotations;

namespace RealEstateAPI.Application.DTOs.Auth
{
    public class ChangeEmailRequestDto
    {
        [Required(ErrorMessage = "New email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(255)]
        public string NewEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Current password is required")]
        public string CurrentPassword { get; set; } = string.Empty;
    }
}
