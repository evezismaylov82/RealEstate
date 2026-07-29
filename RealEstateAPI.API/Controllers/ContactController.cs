using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public ContactController(IUnitOfWork unitOfWork, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
        }

        [HttpPost]
        public async Task<IActionResult> SendContactMessage([FromBody] ContactMessageDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
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

                try
                {
                    await _emailService.SendContactMessageNotificationAsync(
                        "admin@realestate.com",
                        dto.Name,
                        dto.Email,
                        dto.Message
                    );
                }
                catch
                {
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
                return StatusCode(500, new { message = "Error sending message", error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("my-messages")]
        public async Task<IActionResult> GetMyMessages()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var messages = await _unitOfWork.ContactMessages.GetMessagesByUserIdAsync(userId);
                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving messages", error = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("unread")]
        public async Task<IActionResult> GetUnreadMessages()
        {
            try
            {
                var messages = await _unitOfWork.ContactMessages.GetUnreadMessageAsync();
                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving messages", error = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/mark-read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                await _unitOfWork.ContactMessages.MarkAsReadAsync(id);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { message = "Message marked as read" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error marking message", error = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/reply")]
        public async Task<IActionResult> ReplyToMessage(int id, [FromBody] ReplyMessageDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                await _unitOfWork.ContactMessages.ReplyToMessageAsync(id, dto.ReplyMessage, userId);
                await _unitOfWork.SaveChangesAsync();

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
                    }
                    catch
                    {
                    }
                }

                return Ok(new { message = "Reply sent successfully" });
            }
            catch (Exception ex)
            {
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
