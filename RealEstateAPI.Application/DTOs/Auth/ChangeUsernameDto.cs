using System.ComponentModel.DataAnnotations;

namespace RealEstateAPI.Application.DTOs.Auth
{
    public class ChangeUsernameDto
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 30 characters")]
        [RegularExpression(@"^[a-zA-Z0-9_.]+$",
            ErrorMessage = "Username can only contain letters, numbers, dots and underscores")]
        public string Username { get; set; } = string.Empty;
    }
}
