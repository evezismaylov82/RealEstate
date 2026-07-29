using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RealEstateAPI.Application.DTOs.Property;
using RealEstateAPI.Domain.Interfaces.Repositories;
using System.Security.Claims;

namespace RealEstateAPI.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class FavoritesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public FavoritesController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<List<PropertyDto>>> GetMyFavorites()
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var favoriteProperties = await _unitOfWork.Favorites.GetFavoritePropertiesByUserIdAsync(userId.Value);
                var propertyDtos = _mapper.Map<List<PropertyDto>>(favoriteProperties);

                return Ok(propertyDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving favorites", error = ex.Message });
            }
        }

        [HttpPost("{propertyId}")]
        public async Task<IActionResult> ToggleFavorite(int propertyId)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var property = await _unitOfWork.Properties.GetByIdAsync(propertyId);
                if (property == null)
                {
                    return NotFound(new { message = "Property not found" });
                }

                var isAdded = await _unitOfWork.Favorites.ToggleFavoriteAsync(userId.Value, propertyId);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = isAdded ? "Added to favorites" : "Removed from favorites",
                    isAdded = isAdded
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error toggling favorite", error = ex.Message });
            }
        }

        [HttpGet("check/{propertyId}")]
        public async Task<ActionResult<bool>> IsFavorite(int propertyId)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var isFavorite = await _unitOfWork.Favorites.IsFavoriteAsync(userId.Value, propertyId);
                return Ok(new { isFavorite });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error checking favorite", error = ex.Message });
            }
        }

        private int? GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return null;
            }
            return userId;
        }
    }
}
