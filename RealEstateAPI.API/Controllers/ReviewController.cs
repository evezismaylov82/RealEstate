using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateAPI.Application.DTOs.Review;
using RealEstateAPI.Domain.Entities;
using RealEstateAPI.Domain.Interfaces.Repositories;
using System.Security.Claims;

namespace RealEstateAPI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ReviewController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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

        private static ReviewDto ToDto(Review r) => new()
        {
            Id = r.Id,
            ReviewerUserId = r.ReviewerUserId,
            ReviewerFullName = r.ReviewerUser != null ? $"{r.ReviewerUser.FirstName} {r.ReviewerUser.LastName}" : string.Empty,
            PropertyId = r.PropertyId,
            PropertyTitle = r.Property?.Title,
            RevieweeUserId = r.RevieweeUserId,
            RevieweeFullName = r.RevieweeUser != null ? $"{r.RevieweeUser.FirstName} {r.RevieweeUser.LastName}" : null,
            Rating = r.Rating,
            Comment = r.Comment,
            IsApproved = r.IsApproved,
            CreatedAt = r.CreatedAt
        };

        [HttpGet("property/{propertyId}")]
        public async Task<ActionResult<object>> GetForProperty(int propertyId)
        {
            try
            {
                var reviews = await _unitOfWork.Reviews.GetByPropertyIdAsync(propertyId);
                var averageRating = await _unitOfWork.Reviews.GetAveragePropertyRatingAsync(propertyId);

                return Ok(new
                {
                    averageRating,
                    count = reviews.Count(),
                    reviews = reviews.Select(ToDto).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving reviews", error = ex.Message });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<object>> GetForUser(int userId)
        {
            try
            {
                var reviews = await _unitOfWork.Reviews.GetByRevieweeUserIdAsync(userId);
                var averageRating = await _unitOfWork.Reviews.GetAverageUserRatingAsync(userId);

                return Ok(new
                {
                    averageRating,
                    count = reviews.Count(),
                    reviews = reviews.Select(ToDto).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving reviews", error = ex.Message });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<ReviewDto>> Create([FromBody] ReviewCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var hasProperty = dto.PropertyId.HasValue;
                var hasReviewee = dto.RevieweeUserId.HasValue;
                if (hasProperty == hasReviewee)
                {
                    return BadRequest(new { message = "A review must target exactly one of: property, user" });
                }

                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                if (hasProperty)
                {
                    var property = await _unitOfWork.Properties.GetByIdAsync(dto.PropertyId!.Value);
                    if (property == null)
                    {
                        return NotFound(new { message = "Property not found" });
                    }

                    if (await _unitOfWork.Reviews.HasUserReviewedPropertyAsync(userId.Value, dto.PropertyId.Value))
                    {
                        return BadRequest(new { message = "You have already reviewed this property" });
                    }
                }
                else
                {
                    if (dto.RevieweeUserId == userId.Value)
                    {
                        return BadRequest(new { message = "You cannot review yourself" });
                    }

                    var revieweeUser = await _unitOfWork.Users.GetByIdAsync(dto.RevieweeUserId!.Value);
                    if (revieweeUser == null)
                    {
                        return NotFound(new { message = "User not found" });
                    }

                    if (await _unitOfWork.Reviews.HasUserReviewedUserAsync(userId.Value, dto.RevieweeUserId.Value))
                    {
                        return BadRequest(new { message = "You have already reviewed this user" });
                    }
                }

                var review = new Review
                {
                    ReviewerUserId = userId.Value,
                    PropertyId = dto.PropertyId,
                    RevieweeUserId = dto.RevieweeUserId,
                    Rating = dto.Rating,
                    Comment = dto.Comment,
                    IsApproved = true
                };

                await _unitOfWork.Reviews.AddAsync(review);
                await _unitOfWork.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = review.Id }, ToDto(review));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating review", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ReviewDto>> GetById(int id)
        {
            try
            {
                var review = await _unitOfWork.Reviews.GetByIdAsync(id);
                if (review == null)
                {
                    return NotFound(new { message = "Review not found" });
                }

                return Ok(ToDto(review));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving review", error = ex.Message });
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var review = await _unitOfWork.Reviews.GetByIdAsync(id);
                if (review == null)
                {
                    return NotFound(new { message = "Review not found" });
                }

                if (review.ReviewerUserId != userId.Value && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                _unitOfWork.Reviews.Delete(review);
                await _unitOfWork.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting review", error = ex.Message });
            }
        }
    }
}
