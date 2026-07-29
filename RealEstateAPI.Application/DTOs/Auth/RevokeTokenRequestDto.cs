using System.ComponentModel.DataAnnotations;

namespace RealEstateAPI.Application.DTOs.Auth
{
    public class RevokeTokenRequestDto
    {

        public string? RefreshToken { get; set; }
    }
}
