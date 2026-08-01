using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RealEstateAPI.Domain.Entities;
using RealEstateAPI.Domain.Interfaces.Repositories;
using RealEstateAPI.Infrastructure.Services;
using System.Security.Claims;

namespace RealEstateAPI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly ILogger<ContactController> _logger;

        public ContactController(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            ILogger<ContactController> logger)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> SendContactMessage([FromBody] ContactMessageDto dto)
        {
            _logger.LogInformation("SendContactMessage request received from Email: {Email}.", dto?.Email);
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("SendContactMessage failed due to invalid ModelState for Email: {Email}.", dto?.Email);
                    return BadRequest(ModelState);
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int? userId = null;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int uid))
                {
                    userId = uid;
                }

                var message = new ContactMessage
                {
                    UserId = userId,
                    PropertyId = dto.PropertyId,
                    Name = dto.Name,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    Subject = dto.Subject,
                    Message = dto.Message
                };

                await _unitOfWork.ContactMessages.AddAsync(message);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Contact message created with Id: {MessageId} for PropertyId: {PropertyId}.", message.Id, dto.PropertyId);

                try
                {
                    await _emailService.SendContactMessageNotificationAsync(
                        "admin@realestate.com",
                        dto.Name,
                        dto.Email,
                        dto.Message
                    );
                    _logger.LogInformation("Contact notification email sent to admin for MessageId: {MessageId}.", message.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send contact notification email for MessageId: {MessageId}.", message.Id);
                }

                return Ok(new
                {
                    success = true,
                    message = "Your message has been sent successfully. We will contact you soon.",
                    messageId = message.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in SendContactMessage for Email: {Email}.", dto?.Email);
                return StatusCode(500, new { message = "Error sending message", error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("my-messages")]
        public async Task<IActionResult> GetMyMessages()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("GetMyMessages request received.");
            try
            {
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("GetMyMessages failed. Invalid token claim.");
                    return Unauthorized(new { message = "Invalid token" });
                }

                var messages = await _unitOfWork.ContactMessages.GetMessagesByUserIdAsync(userId);
                _logger.LogInformation("Successfully retrieved contact messages for UserId: {UserId}.", userId);
                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetMyMessages.");
                return StatusCode(500, new { message = "Error retrieving messages", error = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("unread")]
        public async Task<IActionResult> GetUnreadMessages()
        {
            _logger.LogInformation("GetUnreadMessages request received by Admin.");
            try
            {
                var messages = await _unitOfWork.ContactMessages.GetUnreadMessageAsync();
                _logger.LogInformation("Successfully retrieved unread contact messages.");
                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetUnreadMessages.");
                return StatusCode(500, new { message = "Error retrieving messages", error = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/mark-read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            _logger.LogInformation("MarkAsRead request received for MessageId: {MessageId}.", id);
            try
            {
                await _unitOfWork.ContactMessages.MarkAsReadAsync(id);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully marked MessageId: {MessageId} as read.", id);
                return Ok(new { message = "Message marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in MarkAsRead for MessageId: {MessageId}.", id);
                return StatusCode(500, new { message = "Error marking message", error = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/reply")]
        public async Task<IActionResult> ReplyToMessage(int id, [FromBody] ReplyMessageDto dto)
        {
            _logger.LogInformation("ReplyToMessage request received for MessageId: {MessageId}.", id);
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("ReplyToMessage failed. Invalid token claim.");
                    return Unauthorized(new { message = "Invalid token" });
                }

                await _unitOfWork.ContactMessages.ReplyToMessageAsync(id, dto.ReplyMessage, userId);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully saved reply for MessageId: {MessageId} by AdminId: {UserId}.", id, userId);

                var message = await _unitOfWork.ContactMessages.GetByIdAsync(id);
                if (message != null)
                {
                    try
                    {
                        await _emailService.SendEmailAsync(
                            message.Email,
                            $"Re: {message.Subject}",
                            dto.ReplyMessage
                        );
                        _logger.LogInformation("Reply email sent to {Email} for MessageId: {MessageId}.", message.Email, id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send reply email to {Email} for MessageId: {MessageId}.", message.Email, id);
                    }
                }

                return Ok(new { message = "Reply sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in ReplyToMessage for MessageId: {MessageId}.", id);
                return StatusCode(500, new { message = "Error replying to message", error = ex.Message });
            }
        }
    }

    public class ContactMessageDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.EmailAddress]
        public string Email { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Phone]
        public string? PhoneNumber { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        public int? PropertyId { get; set; }
    }

    public class ReplyMessageDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string ReplyMessage { get; set; } = string.Empty;
    }
}