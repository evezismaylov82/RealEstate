using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateAPI.Application.DTOs.Auth;
using RealEstateAPI.Domain.Interfaces.Repositories;
using System.Security.Claims;

namespace RealEstateAPI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UserController(IUnitOfWork unitOfWork, IMapper mapper)
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

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<UserDto>> GetMyProfile()
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(_mapper.Map<UserDto>(user));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving profile", error = ex.Message });
            }
        }

        [Authorize]
        [HttpPut("me")]
        public async Task<ActionResult<UserDto>> UpdateMyProfile([FromBody] UpdateProfileDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                if (!string.IsNullOrWhiteSpace(dto.FirstName))
                {
                    user.FirstName = dto.FirstName;
                }

                if (!string.IsNullOrWhiteSpace(dto.LastName))
                {
                    user.LastName = dto.LastName;
                }

                if (dto.PhoneNumber != null)
                {
                    user.PhoneNumber = dto.PhoneNumber;
                }

                if (dto.ProfileImageUrl != null)
                {
                    user.ProfileImageUrl = dto.ProfileImageUrl;
                }

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                return Ok(_mapper.Map<UserDto>(user));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating profile", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetPublicProfile(int id)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var averageRating = await _unitOfWork.Reviews.GetAverageUserRatingAsync(id);
                var userDto = _mapper.Map<UserDto>(user);

                return Ok(new
                {
                    userDto.Id,
                    userDto.FirstName,
                    userDto.LastName,
                    userDto.FullName,
                    userDto.ProfileImageUrl,
                    userDto.Role,
                    userDto.CreatedAt,
                    averageRating
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving user", error = ex.Message });
            }
        }
    }
}
