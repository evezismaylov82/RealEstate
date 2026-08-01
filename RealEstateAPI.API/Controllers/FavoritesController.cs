using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<FavoritesController> _logger;

        public FavoritesController(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<FavoritesController> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<PropertyDto>>> GetMyFavorites()
        {
            var userId = GetUserId();
            _logger.LogInformation("GetMyFavorites request received for UserId: {UserId}.", userId);
            try
            {
                if (userId == null)
                {
                    _logger.LogWarning("GetMyFavorites failed. Invalid token claim.");
                    return Unauthorized(new { message = "Invalid token" });
                }

                var favoriteProperties = await _unitOfWork.Favorites.GetFavoritePropertiesByUserIdAsync(userId.Value);
                var propertyDtos = _mapper.Map<List<PropertyDto>>(favoriteProperties);

                _logger.LogInformation("Successfully retrieved {Count} favorite properties for UserId: {UserId}.", propertyDtos.Count, userId.Value);
                return Ok(propertyDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetMyFavorites for UserId: {UserId}.", userId);
                return StatusCode(500, new { message = "Error retrieving favorites", error = ex.Message });
            }
        }

        [HttpPost("{propertyId}")]
        public async Task<IActionResult> ToggleFavorite(int propertyId)
        {
            var userId = GetUserId();
            _logger.LogInformation("ToggleFavorite request received for PropertyId: {PropertyId} by UserId: {UserId}.", propertyId, userId);
            try
            {
                if (userId == null)
                {
                    _logger.LogWarning("ToggleFavorite failed. Invalid token claim.");
                    return Unauthorized(new { message = "Invalid token" });
                }

                var property = await _unitOfWork.Properties.GetByIdAsync(propertyId);
                if (property == null)
                {
                    _logger.LogWarning("ToggleFavorite failed. Property with Id {PropertyId} not found.", propertyId);
                    return NotFound(new { message = "Property not found" });
                }

                var isAdded = await _unitOfWork.Favorites.ToggleFavoriteAsync(userId.Value, propertyId);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("ToggleFavorite completed. PropertyId: {PropertyId} is now {Status} for UserId: {UserId}.",
                    propertyId, isAdded ? "Added to favorites" : "Removed from favorites", userId.Value);

                return Ok(new
                {
                    success = true,
                    message = isAdded ? "Added to favorites" : "Removed from favorites",
                    isAdded = isAdded
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in ToggleFavorite for PropertyId: {PropertyId} and UserId: {UserId}.", propertyId, userId);
                return StatusCode(500, new { message = "Error toggling favorite", error = ex.Message });
            }
        }

        [HttpGet("check/{propertyId}")]
        public async Task<ActionResult<bool>> IsFavorite(int propertyId)
        {
            var userId = GetUserId();
            _logger.LogInformation("IsFavorite check request received for PropertyId: {PropertyId} by UserId: {UserId}.", propertyId, userId);
            try
            {
                if (userId == null)
                {
                    _logger.LogWarning("IsFavorite check failed. Invalid token claim.");
                    return Unauthorized(new { message = "Invalid token" });
                }

                var isFavorite = await _unitOfWork.Favorites.IsFavoriteAsync(userId.Value, propertyId);
                _logger.LogInformation("IsFavorite check result for PropertyId: {PropertyId} and UserId: {UserId} is {IsFavorite}.", propertyId, userId.Value, isFavorite);
                return Ok(new { isFavorite });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in IsFavorite for PropertyId: {PropertyId} and UserId: {UserId}.", propertyId, userId);
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