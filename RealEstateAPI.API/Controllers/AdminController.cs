using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RealEstateAPI.Application.DTOs.Auth;
using RealEstateAPI.Application.DTOs.Property;
using RealEstateAPI.Application.DTOs.Role;
using RealEstateAPI.Domain.Enums;
using RealEstateAPI.Domain.Interfaces.Repositories;

namespace RealEstateAPI.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IUnitOfWork unitOfWork, IMapper mapper, ILogger<AdminController> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet("users")]
        public async Task<ActionResult<List<UserDto>>> GetAllUsers()
        {
            _logger.LogInformation("GetAllUsers request received.");
            try
            {
                var users = await _unitOfWork.Users.GetAllAsync();
                var userDtos = _mapper.Map<List<UserDto>>(users);

                _logger.LogInformation("Successfully retrieved {Count} users.", userDtos.Count);
                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetAllUsers endpoint.");
                return StatusCode(500, new { message = "Error retrieving users", error = ex.Message });
            }
        }

        [HttpGet("users/{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            _logger.LogInformation("GetUser request received for UserId: {UserId}.", id);
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("GetUser failed. User with Id {UserId} was not found.", id);
                    return NotFound(new { message = "User not found" });
                }

                var userDto = _mapper.Map<UserDto>(user);
                _logger.LogInformation("Successfully retrieved user details for UserId: {UserId}.", id);
                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetUser endpoint for UserId: {UserId}.", id);
                return StatusCode(500, new { message = "Error retrieving user", error = ex.Message });
            }
        }

        [HttpPut("users/{id}/change-role")]
        public async Task<IActionResult> ChangeUserRole(int id, [FromBody] ChangeRoleDto dto)
        {
            _logger.LogInformation("ChangeUserRole request received for UserId: {UserId} to Role: {Role}.", id, dto?.Role);
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("ChangeUserRole failed. User with Id {UserId} was not found.", id);
                    return NotFound(new { message = "User not found" });
                }

                user.Role = dto.Role;
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully changed role for UserId: {UserId} to {Role}.", id, dto.Role);
                return Ok(new { message = $"User role changed to {dto.Role}", userId = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in ChangeUserRole endpoint for UserId: {UserId}.", id);
                return StatusCode(500, new { message = "Error changing role", error = ex.Message });
            }
        }

        [HttpPut("users/{id}/lock")]
        public async Task<IActionResult> LockUser(int id, [FromBody] LockUserDto dto)
        {
            _logger.LogInformation("LockUser request received for UserId: {UserId}.", id);
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("LockUser failed. User with Id {UserId} was not found.", id);
                    return NotFound(new { message = "User not found" });
                }

                if (user.IsAdmin)
                {
                    _logger.LogWarning("LockUser rejected. Attempted to lock admin user with UserId: {UserId}.", id);
                    return BadRequest(new { message = "Admin accounts cannot be locked" });
                }

                user.IsLocked = true;
                user.LockedUntil = dto.LockedUntil;
                user.LockReason = dto.Reason;
                _unitOfWork.Users.Update(user);

                await _unitOfWork.RefreshTokens.RevokeAllActiveTokensForUserAsync(
                    id, reason: "Account locked by administrator");

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully locked user with UserId: {UserId} until {LockedUntil}.", id, user.LockedUntil);
                return Ok(new
                {
                    message = "User locked",
                    userId = id,
                    lockedUntil = user.LockedUntil,
                    reason = user.LockReason
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in LockUser endpoint for UserId: {UserId}.", id);
                return StatusCode(500, new { message = "Error locking user", error = ex.Message });
            }
        }

        [HttpPut("users/{id}/unlock")]
        public async Task<IActionResult> UnlockUser(int id)
        {
            _logger.LogInformation("UnlockUser request received for UserId: {UserId}.", id);
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("UnlockUser failed. User with Id {UserId} was not found.", id);
                    return NotFound(new { message = "User not found" });
                }

                user.IsLocked = false;
                user.LockedUntil = null;
                user.LockReason = null;
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully unlocked user with UserId: {UserId}.", id);
                return Ok(new { message = "User unlocked", userId = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in UnlockUser endpoint for UserId: {UserId}.", id);
                return StatusCode(500, new { message = "Error unlocking user", error = ex.Message });
            }
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            _logger.LogInformation("DeleteUser request received for UserId: {UserId}.", id);
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("DeleteUser failed. User with Id {UserId} was not found.", id);
                    return NotFound(new { message = "User not found" });
                }

                _unitOfWork.Users.Delete(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted user with UserId: {UserId}.", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in DeleteUser endpoint for UserId: {UserId}.", id);
                return StatusCode(500, new { message = "Error deleting user", error = ex.Message });
            }
        }

        [HttpGet("properties/pending")]
        public async Task<ActionResult<List<PropertyDto>>> GetPendingProperties()
        {
            _logger.LogInformation("GetPendingProperties request received.");
            try
            {
                var properties = await _unitOfWork.Properties.GetAsync(p => !p.IsPublished);
                var propertyDtos = _mapper.Map<List<PropertyDto>>(properties);

                _logger.LogInformation("Successfully retrieved {Count} pending properties.", propertyDtos.Count);
                return Ok(propertyDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetPendingProperties endpoint.");
                return StatusCode(500, new { message = "Error retrieving pending properties", error = ex.Message });
            }
        }

        [HttpPut("properties/{id}/approve")]
        public async Task<IActionResult> ApproveProperty(int id)
        {
            _logger.LogInformation("ApproveProperty request received for PropertyId: {PropertyId}.", id);
            try
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(id);
                if (property == null)
                {
                    _logger.LogWarning("ApproveProperty failed. Property with Id {PropertyId} was not found.", id);
                    return NotFound(new { message = "Property not found" });
                }

                property.IsPublished = true;
                _unitOfWork.Properties.Update(property);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully approved property with PropertyId: {PropertyId}.", id);
                return Ok(new { message = "Property approved", propertyId = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in ApproveProperty endpoint for PropertyId: {PropertyId}.", id);
                return StatusCode(500, new { message = "Error approving property", error = ex.Message });
            }
        }

        [HttpPut("properties/{id}/reject")]
        public async Task<IActionResult> RejectProperty(int id)
        {
            _logger.LogInformation("RejectProperty request received for PropertyId: {PropertyId}.", id);
            try
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(id);
                if (property == null)
                {
                    _logger.LogWarning("RejectProperty failed. Property with Id {PropertyId} was not found.", id);
                    return NotFound(new { message = "Property not found" });
                }

                property.IsPublished = false;
                _unitOfWork.Properties.Update(property);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully rejected property with PropertyId: {PropertyId}.", id);
                return Ok(new { message = "Property rejected", propertyId = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in RejectProperty endpoint for PropertyId: {PropertyId}.", id);
                return StatusCode(500, new { message = "Error rejecting property", error = ex.Message });
            }
        }

        [HttpPut("properties/{id}/feature")]
        public async Task<IActionResult> FeatureProperty(int id, [FromBody] FeaturePropertyDto dto)
        {
            _logger.LogInformation("FeatureProperty request received for PropertyId: {PropertyId} with IsFeatured: {IsFeatured}.", id, dto?.IsFeatured);
            try
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(id);
                if (property == null)
                {
                    _logger.LogWarning("FeatureProperty failed. Property with Id {PropertyId} was not found.", id);
                    return NotFound(new { message = "Property not found" });
                }

                property.IsFeatured = dto.IsFeatured;
                _unitOfWork.Properties.Update(property);
                await _unitOfWork.SaveChangesAsync();

                var message = dto.IsFeatured ? "Property featured" : "Property unfeatured";
                _logger.LogInformation("Successfully updated feature status for PropertyId: {PropertyId}. New status: {IsFeatured}.", id, dto.IsFeatured);
                return Ok(new { message, propertyId = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in FeatureProperty endpoint for PropertyId: {PropertyId}.", id);
                return StatusCode(500, new { message = "Error featuring property", error = ex.Message });
            }
        }

        [HttpDelete("properties/{id}")]
        public async Task<IActionResult> DeleteProperty(int id)
        {
            _logger.LogInformation("DeleteProperty request received for PropertyId: {PropertyId}.", id);
            try
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(id);
                if (property == null)
                {
                    _logger.LogWarning("DeleteProperty failed. Property with Id {PropertyId} was not found.", id);
                    return NotFound(new { message = "Property not found" });
                }

                _unitOfWork.Properties.Delete(property);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted property with PropertyId: {PropertyId}.", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in DeleteProperty endpoint for PropertyId: {PropertyId}.", id);
                return StatusCode(500, new { message = "Error deleting property", error = ex.Message });
            }
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            _logger.LogInformation("GetStatistics request received.");
            try
            {
                var totalUsers = await _unitOfWork.Users.CountAsync();
                var totalProperties = await _unitOfWork.Properties.CountAsync();
                var pendingProperties = await _unitOfWork.Properties.CountAsync(p => !p.IsPublished);
                var totalPayments = await _unitOfWork.Payments.CountAsync();

                var statistics = new
                {
                    totalUsers,
                    totalProperties,
                    pendingProperties,
                    totalPayments,
                    generatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Successfully calculated system statistics.");
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetStatistics endpoint.");
                return StatusCode(500, new { message = "Error retrieving statistics", error = ex.Message });
            }
        }
    }

    public class ChangeRoleDto
    {
        public UserRole Role { get; set; }
    }

    public class FeaturePropertyDto
    {
        public bool IsFeatured { get; set; }
    }
}