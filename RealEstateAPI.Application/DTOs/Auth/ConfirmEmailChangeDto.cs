using System.ComponentModel.DataAnnotations;

namespace RealEstateAPI.Application.DTOs.Auth
{
    public class ConfirmEmailChangeDto
    {
        [Required(ErrorMessage = "Token is required")]
        public string Token { get; set; } = string.Empty;
    }
}
