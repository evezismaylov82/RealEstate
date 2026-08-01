using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            JwtHelper jwtHelper,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _jwtHelper = jwtHelper;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
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
            _logger.LogInformation("Issued new refresh token for UserId: {UserId}.", user.Id);
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
            _logger.LogInformation("Register request received for Email: {Email}.", registerDto?.Email);
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Register failed due to invalid ModelState for Email: {Email}.", registerDto?.Email);
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
                    _logger.LogWarning("Register failed. Email {Email} is already registered.", registerDto.Email);
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
                    _logger.LogInformation("Verification email sent to {Email}.", user.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send verification email to {Email}.", user.Email);
                }

                var token = _jwtHelper.GenerateToken(user, rememberMe: false);
                var refreshToken = await IssueRefreshTokenAsync(user);
                await _unitOfWork.SaveChangesAsync();

                var userDto = _mapper.Map<UserDto>(user);

                _logger.LogInformation("Successfully registered user with UserId: {UserId}.", user.Id);
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
                _logger.LogError(ex, "Error occurred during registration for Email: {Email}.", registerDto?.Email);
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
            _logger.LogInformation("Login request received for Email: {Email}.", loginDto?.Email);
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Login failed due to invalid ModelState for Email: {Email}.", loginDto?.Email);
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
                    _logger.LogWarning("Login failed. User with Email: {Email} not found.", loginDto.Email);
                    return Unauthorized(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid email or password",
                        Errors = new List<string> { "The email or password you entered is incorrect" }
                    });
                }

                if (!PasswordHelper.VerifyPassword(loginDto.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Login failed. Invalid password for UserId: {UserId}.", user.Id);
                    return Unauthorized(new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid email or password",
                        Errors = new List<string> { "The email or password you entered is incorrect" }
                    });
                }

                if (user.IsCurrentlyLocked)
                {
                    _logger.LogWarning("Login blocked. Account is locked for UserId: {UserId}.", user.Id);
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

                _logger.LogInformation("Login successful for UserId: {UserId}.", user.Id);
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
                _logger.LogError(ex, "Error occurred during login for Email: {Email}.", loginDto?.Email);
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
            _logger.LogInformation("VerifyEmail request received.");
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogWarning("VerifyEmail failed. Token is null or empty.");
                    return BadRequest(new { success = false, message = "Token is required" });
                }

                var user = await _unitOfWork.Users.GetByEmailVerificationTokenAsync(token);
                if (user == null)
                {
                    _logger.LogWarning("VerifyEmail failed. Invalid or expired token provided.");
                    return BadRequest(new { success = false, message = "Invalid or expired token" });
                }

                user.IsEmailVerified = true;
                user.EmailVerificationToken = null;
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Email successfully verified for UserId: {UserId}.", user.Id);
                return Ok(new { success = true, message = "Email verified successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during email verification.");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            _logger.LogInformation("ForgotPassword request received for Email: {Email}.", dto?.Email);
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ForgotPassword failed due to invalid ModelState for Email: {Email}.", dto?.Email);
                    return BadRequest(ModelState);
                }

                var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email);
                if (user == null)
                {
                    _logger.LogInformation("ForgotPassword request processed for non-existing email: {Email}.", dto.Email);
                    return Ok(new { success = true, message = "If the email exists, a reset link has been sent" });
                }

                user.PasswordResetToken = PasswordHelper.GenerateRandomToken();
                user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(24);

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                try
                {
                    await _emailService.SendPasswordResetEmailAsync(user.Email, user.PasswordResetToken, GetClientBaseUrl());
                    _logger.LogInformation("Password reset email sent to {Email}.", user.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send password reset email to {Email}.", user.Email);
                }

                return Ok(new { success = true, message = "If the email exists, a reset link has been sent" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in ForgotPassword for Email: {Email}.", dto?.Email);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("reset-password")]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            _logger.LogInformation("ResetPassword request received.");
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ResetPassword failed due to invalid ModelState.");
                    return BadRequest(ModelState);
                }

                var user = await _unitOfWork.Users.GetByPasswordResetTokenAsync(dto.Token);
                if (user == null)
                {
                    _logger.LogWarning("ResetPassword failed. Invalid or expired reset token.");
                    return BadRequest(new { success = false, message = "Invalid or expired token" });
                }

                user.PasswordHash = PasswordHelper.HashPassword(dto.NewPassword);
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpiry = null;

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Password successfully reset for UserId: {UserId}.", user.Id);
                return Ok(new { success = true, message = "Password reset successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during ResetPassword.");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("ChangePassword request received for UserId: {UserId}.", userId);
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ChangePassword failed due to invalid ModelState for UserId: {UserId}.", userId);
                    return BadRequest(ModelState);
                }

                if (userId == null)
                {
                    _logger.LogWarning("ChangePassword failed. Invalid token claim for user.");
                    return Unauthorized(new { success = false, message = "Invalid token" });
                }

                var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);
                if (user == null)
                {
                    _logger.LogWarning("ChangePassword failed. User with Id {UserId} not found.", userId.Value);
                    return NotFound(new { success = false, message = "User not found" });
                }

                if (!PasswordHelper.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
                {
                    _logger.LogWarning("ChangePassword failed. Incorrect current password for UserId: {UserId}.", user.Id);
                    return BadRequest(new { success = false, message = "Current password is incorrect" });
                }

                user.PasswordHash = PasswordHelper.HashPassword(dto.NewPassword);
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Password successfully changed for UserId: {UserId}.", user.Id);
                return Ok(new { success = true, message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in ChangePassword for UserId: {UserId}.", userId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto dto)
        {
            _logger.LogInformation("RefreshToken request received.");
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("RefreshToken failed due to invalid ModelState.");
                    return BadRequest(ModelState);
                }

                var existingToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(dto.RefreshToken);
                if (existingToken == null || !existingToken.IsActive)
                {
                    _logger.LogWarning("RefreshToken failed. Token is invalid or expired.");
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
                    _logger.LogWarning("RefreshToken failed. User is null or currently locked (UserId: {UserId}).", existingToken.UserId);
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

                _logger.LogInformation("RefreshToken succeeded for UserId: {UserId}.", user.Id);
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
                _logger.LogError(ex, "Error occurred during RefreshToken.");
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
            var userId = GetCurrentUserId();
            _logger.LogInformation("RevokeToken request received for UserId: {UserId}.", userId);
            try
            {
                if (userId == null)
                {
                    _logger.LogWarning("RevokeToken failed. Invalid token claim.");
                    return Unauthorized(new { success = false, message = "Invalid token" });
                }

                if (string.IsNullOrWhiteSpace(dto.RefreshToken))
                {
                    await _unitOfWork.RefreshTokens.RevokeAllActiveTokensForUserAsync(
                        userId.Value, GetClientIp(), "User signed out of all sessions");
                    _logger.LogInformation("Revoked all active tokens for UserId: {UserId}.", userId.Value);
                }
                else
                {
                    var token = await _unitOfWork.RefreshTokens.GetByTokenAsync(dto.RefreshToken);
                    if (token == null || token.UserId != userId.Value)
                    {
                        _logger.LogWarning("RevokeToken failed. Refresh token not found for UserId: {UserId}.", userId.Value);
                        return NotFound(new { success = false, message = "Refresh token not found" });
                    }

                    if (token.IsActive)
                    {
                        token.RevokedAt = DateTime.UtcNow;
                        token.RevokedByIp = GetClientIp();
                        token.RevokeReason = "User signed out";
                        _unitOfWork.RefreshTokens.Update(token);
                        _logger.LogInformation("Successfully revoked single refresh token for UserId: {UserId}.", userId.Value);
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                return Ok(new { success = true, message = "Token(s) revoked" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during RevokeToken for UserId: {UserId}.", userId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("change-email")]
        public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequestDto dto)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("ChangeEmail request received for UserId: {UserId}.", userId);
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ChangeEmail failed due to invalid ModelState for UserId: {UserId}.", userId);
                    return BadRequest(ModelState);
                }

                if (userId == null)
                {
                    _logger.LogWarning("ChangeEmail failed. Invalid token claim.");
                    return Unauthorized(new { success = false, message = "Invalid token" });
                }

                var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);
                if (user == null)
                {
                    _logger.LogWarning("ChangeEmail failed. User with Id {UserId} not found.", userId.Value);
                    return NotFound(new { success = false, message = "User not found" });
                }

                if (!PasswordHelper.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
                {
                    _logger.LogWarning("ChangeEmail failed. Incorrect current password for UserId: {UserId}.", user.Id);
                    return BadRequest(new { success = false, message = "Current password is incorrect" });
                }

                if (string.Equals(dto.NewEmail, user.Email, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("ChangeEmail failed. New email is identical to current email for UserId: {UserId}.", user.Id);
                    return BadRequest(new { success = false, message = "New email must be different from the current email" });
                }

                if (await _unitOfWork.Users.IsEmailExistsAsync(dto.NewEmail))
                {
                    _logger.LogWarning("ChangeEmail failed. New email {NewEmail} is already in use.", dto.NewEmail);
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
                    _logger.LogInformation("Email change confirmation link sent to {PendingEmail}.", user.PendingEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email change confirmation to {PendingEmail}.", user.PendingEmail);
                }

                return Ok(new
                {
                    success = true,
                    message = "A confirmation link has been sent to your new email address"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during ChangeEmail for UserId: {UserId}.", userId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("confirm-email-change")]
        public async Task<IActionResult> ConfirmEmailChange([FromBody] ConfirmEmailChangeDto dto)
        {
            _logger.LogInformation("ConfirmEmailChange request received.");
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ConfirmEmailChange failed due to invalid ModelState.");
                    return BadRequest(ModelState);
                }

                var user = await _unitOfWork.Users.GetByEmailChangeTokenAsync(dto.Token);
                if (user == null || string.IsNullOrEmpty(user.PendingEmail))
                {
                    _logger.LogWarning("ConfirmEmailChange failed. Invalid or expired token.");
                    return BadRequest(new { success = false, message = "Invalid or expired token" });
                }

                if (await _unitOfWork.Users.IsEmailExistsAsync(user.PendingEmail))
                {
                    _logger.LogWarning("ConfirmEmailChange failed. Pending email {PendingEmail} is already taken.", user.PendingEmail);
                    return BadRequest(new { success = false, message = "This email address is already in use" });
                }

                user.Email = user.PendingEmail;
                user.IsEmailVerified = true;
                user.PendingEmail = null;
                user.EmailChangeToken = null;
                user.EmailChangeTokenExpiry = null;

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Email address successfully updated for UserId: {UserId}.", user.Id);
                return Ok(new { success = true, message = "Email address updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during ConfirmEmailChange.");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpPut("change-username")]
        public async Task<IActionResult> ChangeUsername([FromBody] ChangeUsernameDto dto)
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("ChangeUsername request received for UserId: {UserId} with new Username: {Username}.", userId, dto?.Username);
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ChangeUsername failed due to invalid ModelState for UserId: {UserId}.", userId);
                    return BadRequest(ModelState);
                }

                if (userId == null)
                {
                    _logger.LogWarning("ChangeUsername failed. Invalid token claim.");
                    return Unauthorized(new { success = false, message = "Invalid token" });
                }

                var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);
                if (user == null)
                {
                    _logger.LogWarning("ChangeUsername failed. User with Id {UserId} not found.", userId.Value);
                    return NotFound(new { success = false, message = "User not found" });
                }

                if (await _unitOfWork.Users.IsUsernameExistsAsync(dto.Username))
                {
                    _logger.LogWarning("ChangeUsername failed. Username {Username} is already taken.", dto.Username);
                    return BadRequest(new { success = false, message = "This username is already taken" });
                }

                user.Username = dto.Username;
                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Username updated successfully for UserId: {UserId}.", user.Id);
                return Ok(new { success = true, message = "Username updated successfully", username = user.Username });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during ChangeUsername for UserId: {UserId}.", userId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("test-token")]
        public IActionResult TestToken()
        {
            _logger.LogInformation("TestToken request received.");
            var authHeader = Request.Headers["Authorization"].ToString();

            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                _logger.LogWarning("TestToken failed. Authorization header is missing or invalid.");
                return Unauthorized(new { message = "No token provided" });
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();

            var isValid = _jwtHelper.ValidateToken(token);
            var userId = _jwtHelper.GetUserIdFromToken(token);

            _logger.LogInformation("TestToken evaluated. Valid: {IsValid}, UserId: {UserId}.", isValid, userId);
            return Ok(new
            {
                tokenValid = isValid,
                userId = userId,
                message = isValid ? "Token is valid" : "Token is invalid"
            });
        }
    }
}