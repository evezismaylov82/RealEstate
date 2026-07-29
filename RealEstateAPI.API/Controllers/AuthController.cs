using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RealEstateAPI.Application.DTOs.Auth;
using RealEstateAPI.Domain.Entities;
using RealEstateAPI.Domain.Interfaces.Repositories;
using RealEstateAPI.Infrastructure.Helpers;
using RealEstateAPI.Infrastructure.Services;
using System.Security.Claims;

namespace RealEstateAPI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly JwtHelper _jwtHelper;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public AuthController(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            JwtHelper jwtHelper,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _jwtHelper = jwtHelper;
            _emailService = emailService;
            _configuration = configuration;
        }

        private string GetClientBaseUrl()
        {
            var configuredUrl = _configuration["AppSettings:ClientBaseUrl"];
            if (!string.IsNullOrWhiteSpace(configuredUrl))
            {
                return configuredUrl.TrimEnd('/');
            }

            return $"{Request.Scheme}://{Request.Host}";
        }

        private int RefreshTokenExpirationDays
        {
            get
            {
                var raw = _configuration["JwtSettings:RefreshTokenExpirationDays"];
                return int.TryParse(raw, out var days) ? days : 30;
            }
        }

        private string? GetClientIp() =>
            HttpContext.Connection.RemoteIpAddress?.ToString();

        private async Task<RefreshToken> IssueRefreshTokenAsync(User user)
        {
            var refreshToken = new RefreshToken
            {
                Token = _jwtHelper.GenerateRefreshToken(),
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpirationDays),
                CreatedByIp = GetClientIp()
            };

            await _unitOfWork.RefreshTokens.AddAsync(refreshToken);
            return refreshToken;
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return null;
            }
            return userId;
        }

        [HttpPost("register")]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                var existingUser = await _unitOfWork.Users.GetByEmailAsync(registerDto.Email);
                if (existingUser != null)
                {
                    return BadRequest(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Email already registered",
                        Errors = new List<string> { "This email address is already in use" }
                    });
                }

                var user = _mapper.Map<User>(registerDto);

                user.PasswordHash = PasswordHelper.HashPassword(registerDto.Password);
                user.EmailVerificationToken = PasswordHelper.GenerateRandomToken();
                user.IsEmailVerified = false;

                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();

                try
                {
                    await _emailService.SendVerificationEmailAsync(user.Email, user.EmailVerificationToken, GetClientBaseUrl());
                }
                catch
                {
                }

                var token = _jwtHelper.GenerateToken(user, rememberMe: false);
                var refreshToken = await IssueRefreshTokenAsync(user);
                await _unitOfWork.SaveChangesAsync();

                var userDto = _mapper.Map<UserDto>(user);

                return Ok(new AuthResponseDto
                {
                    Success = true,
                    Message = "Registration successful. Please verify your email.",
                    Token = token,
                    TokenExpiry = DateTime.UtcNow.AddDays(1),
                    RefreshToken = refreshToken.Token,
                    RefreshTokenExpiry = refreshToken.ExpiresAt,
                    User = userDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during registration",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("login")]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Validation failed",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                var user = await _unitOfWork.Users.GetByEmailAsync(loginDto.Email);
                if (user == null)
                {
                    return Unauthorized(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid email or password",
                        Errors = new List<string> { "The email or password you entered is incorrect" }
                    });
                }

                if (!PasswordHelper.VerifyPassword(loginDto.Password, user.PasswordHash))
                {
                    return Unauthorized(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid email or password",
                        Errors = new List<string> { "The email or password you entered is incorrect" }
                    });
                }

                if (user.IsCurrentlyLocked)
                {
                    var lockMessage = user.LockedUntil.HasValue
                        ? $"This account is locked until {user.LockedUntil:u}."
                        : "This account has been locked.";

                    return StatusCode(StatusCodes.Status423Locked, new AuthResponseDto
                    {
                        Success = false,
                        Message = "Account locked",
                        Errors = new List<string> { user.LockReason ?? lockMessage }
                    });
                }

                user.LastLoginAt = DateTime.UtcNow;
                _unitOfWork.Users.Update(user);

                var permissions = await _unitOfWork.Roles.GetPermissionNamesForUserAsync(user.Id);
                var token = _jwtHelper.GenerateToken(user, loginDto.RememberMe, permissions);
                var refreshToken = await IssueRefreshTokenAsync(user);

                await _unitOfWork.SaveChangesAsync();

                var userDto = _mapper.Map<UserDto>(user);

                var tokenExpiry = loginDto.RememberMe
                    ? DateTime.UtcNow.AddDays(30)
                    : DateTime.UtcNow.AddDays(1);

                return Ok(new AuthResponseDto
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,
                    TokenExpiry = tokenExpiry,
                    RefreshToken = refreshToken.Token,
                    RefreshTokenExpiry = refreshToken.ExpiresAt,
                    User = userDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred during login",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return BadRequest(new { success = false, message = "Token is required" });
                }

                var user = await _unitOfWork.Users.GetByEmailVerificationTokenAsync(token);
                if (user == null)
                {
                    return BadRequest(new { success = false, message = "Invalid or expired token" });
                }

                user.IsEmailVerified = true;
                user.EmailVerificationToken = null;
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { success = true, message = "Email verified successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email);
                if (user == null)
                {
                    return Ok(new { success = true, message = "If the email exists, a reset link has been sent" });
                }

                user.PasswordResetToken = PasswordHelper.GenerateRandomToken();
                user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(24);

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                try
                {
                    await _emailService.SendPasswordResetEmailAsync(user.Email, user.PasswordResetToken, GetClientBaseUrl());
                }
                catch
                {
                }

                return Ok(new { success = true, message = "If the email exists, a reset link has been sent" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("reset-password")]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _unitOfWork.Users.GetByPasswordResetTokenAsync(dto.Token);
                if (user == null)
                {
                    return BadRequest(new { success = false, message = "Invalid or expired token" });
                }

                user.PasswordHash = PasswordHelper.HashPassword(dto.NewPassword);
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpiry = null;

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { success = true, message = "Password reset successful" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { success = false, message = "Invalid token" });
                }

                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                if (!PasswordHelper.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
                {
                    return BadRequest(new { success = false, message = "Current password is incorrect" });
                }

                user.PasswordHash = PasswordHelper.HashPassword(dto.NewPassword);
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { success = true, message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existingToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(dto.RefreshToken);
                if (existingToken == null || !existingToken.IsActive)
                {
                    return Unauthorized(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid or expired refresh token",
                        Errors = new List<string> { "Please log in again" }
                    });
                }

                var user = existingToken.User ?? await _unitOfWork.Users.GetByIdAsync(existingToken.UserId);
                if (user == null || user.IsCurrentlyLocked)
                {
                    return Unauthorized(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Account unavailable",
                        Errors = new List<string> { "Please log in again" }
                    });
                }

                var newRefreshToken = new RefreshToken
                {
                    Token = _jwtHelper.GenerateRefreshToken(),
                    UserId = user.Id,
                    ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpirationDays),
                    CreatedByIp = GetClientIp()
                };
                await _unitOfWork.RefreshTokens.AddAsync(newRefreshToken);

                existingToken.RevokedAt = DateTime.UtcNow;
                existingToken.RevokedByIp = GetClientIp();
                existingToken.ReplacedByToken = newRefreshToken.Token;
                existingToken.RevokeReason = "Replaced by new token";
                _unitOfWork.RefreshTokens.Update(existingToken);

                var permissions = await _unitOfWork.Roles.GetPermissionNamesForUserAsync(user.Id);
                var newAccessToken = _jwtHelper.GenerateToken(user, rememberMe: false, permissions);

                await _unitOfWork.SaveChangesAsync();

                return Ok(new AuthResponseDto
                {
                    Success = true,
                    Message = "Token refreshed",
                    Token = newAccessToken,
                    TokenExpiry = DateTime.UtcNow.AddDays(1),
                    RefreshToken = newRefreshToken.Token,
                    RefreshTokenExpiry = newRefreshToken.ExpiresAt,
                    User = _mapper.Map<UserDto>(user)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponseDto
                {
                    Success = false,
                    Message = "An error occurred while refreshing the token",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [Authorize]
        [HttpPost("revoke-token")]
        public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequestDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new { success = false, message = "Invalid token" });
                }

                if (string.IsNullOrWhiteSpace(dto.RefreshToken))
                {
                    await _unitOfWork.RefreshTokens.RevokeAllActiveTokensForUserAsync(
                        userId.Value, GetClientIp(), "User signed out of all sessions");
                }
                else
                {
                    var token = await _unitOfWork.RefreshTokens.GetByTokenAsync(dto.RefreshToken);
                    if (token == null || token.UserId != userId.Value)
                    {
                        return NotFound(new { success = false, message = "Refresh token not found" });
                    }

                    if (token.IsActive)
                    {
                        token.RevokedAt = DateTime.UtcNow;
                        token.RevokedByIp = GetClientIp();
                        token.RevokeReason = "User signed out";
                        _unitOfWork.RefreshTokens.Update(token);
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                return Ok(new { success = true, message = "Token(s) revoked" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("change-email")]
        public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequestDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new { success = false, message = "Invalid token" });
                }

                var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                if (!PasswordHelper.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
                {
                    return BadRequest(new { success = false, message = "Current password is incorrect" });
                }

                if (string.Equals(dto.NewEmail, user.Email, StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { success = false, message = "New email must be different from the current email" });
                }

                if (await _unitOfWork.Users.IsEmailExistsAsync(dto.NewEmail))
                {
                    return BadRequest(new { success = false, message = "This email address is already in use" });
                }

                user.PendingEmail = dto.NewEmail;
                user.EmailChangeToken = PasswordHelper.GenerateRandomToken();
                user.EmailChangeTokenExpiry = DateTime.UtcNow.AddHours(24);

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                try
                {
                    await _emailService.SendVerificationEmailAsync(
                        user.PendingEmail, user.EmailChangeToken, GetClientBaseUrl());
                }
                catch
                {
                }

                return Ok(new
                {
                    success = true,
                    message = "A confirmation link has been sent to your new email address"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("confirm-email-change")]
        public async Task<IActionResult> ConfirmEmailChange([FromBody] ConfirmEmailChangeDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _unitOfWork.Users.GetByEmailChangeTokenAsync(dto.Token);
                if (user == null || string.IsNullOrEmpty(user.PendingEmail))
                {
                    return BadRequest(new { success = false, message = "Invalid or expired token" });
                }

                if (await _unitOfWork.Users.IsEmailExistsAsync(user.PendingEmail))
                {
                    return BadRequest(new { success = false, message = "This email address is already in use" });
                }

                user.Email = user.PendingEmail;
                user.IsEmailVerified = true;
                user.PendingEmail = null;
                user.EmailChangeToken = null;
                user.EmailChangeTokenExpiry = null;

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { success = true, message = "Email address updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpPut("change-username")]
        public async Task<IActionResult> ChangeUsername([FromBody] ChangeUsernameDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new { success = false, message = "Invalid token" });
                }

                var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                if (await _unitOfWork.Users.IsUsernameExistsAsync(dto.Username))
                {
                    return BadRequest(new { success = false, message = "This username is already taken" });
                }

                user.Username = dto.Username;
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { success = true, message = "Username updated successfully", username = user.Username });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("test-token")]
        public IActionResult TestToken()
        {
            var authHeader = Request.Headers["Authorization"].ToString();

            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { message = "No token provided" });
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();

            var isValid = _jwtHelper.ValidateToken(token);
            var userId = _jwtHelper.GetUserIdFromToken(token);

            return Ok(new
            {
                tokenValid = isValid,
                userId = userId,
                message = isValid ? "Token is valid" : "Token is invalid"
            });
        }
    }
}
