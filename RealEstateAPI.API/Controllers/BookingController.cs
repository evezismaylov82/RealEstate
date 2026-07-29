using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateAPI.Application.DTOs.Booking;
using RealEstateAPI.Domain.Entities;
using RealEstateAPI.Domain.Enums;
using RealEstateAPI.Domain.Interfaces.Repositories;
using System.Security.Claims;

namespace RealEstateAPI.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public BookingController(IUnitOfWork unitOfWork, IMapper mapper)
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

        private static BookingDto ToDto(Booking b) => new()
        {
            Id = b.Id,
            PropertyId = b.PropertyId,
            PropertyTitle = b.Property?.Title ?? string.Empty,
            UserId = b.UserId,
            UserFullName = b.User != null ? $"{b.User.FirstName} {b.User.LastName}" : string.Empty,
            RequestedDateTime = b.RequestedDateTime,
            Status = b.Status.ToString(),
            Notes = b.Notes,
            ContactPhone = b.ContactPhone,
            ContactEmail = b.ContactEmail,
            CancellationReason = b.CancellationReason,
            RespondedAt = b.RespondedAt,
            CreatedAt = b.CreatedAt
        };

        [HttpPost]
        public async Task<ActionResult<BookingDto>> Create([FromBody] BookingCreateDto dto)
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

                if (dto.RequestedDateTime <= DateTime.UtcNow)
                {
                    return BadRequest(new { message = "Requested date and time must be in the future" });
                }

                var property = await _unitOfWork.Properties.GetByIdAsync(dto.PropertyId);
                if (property == null)
                {
                    return NotFound(new { message = "Property not found" });
                }

                if (await _unitOfWork.Bookings.HasOverlappingBookingAsync(dto.PropertyId, dto.RequestedDateTime))
                {
                    return BadRequest(new { message = "This time slot is already booked. Please choose another time." });
                }

                var booking = new Booking
                {
                    PropertyId = dto.PropertyId,
                    UserId = userId.Value,
                    RequestedDateTime = dto.RequestedDateTime,
                    Notes = dto.Notes,
                    ContactPhone = dto.ContactPhone,
                    ContactEmail = dto.ContactEmail,
                    Status = BookingStatus.Pending
                };

                await _unitOfWork.Bookings.AddAsync(booking);
                await _unitOfWork.SaveChangesAsync();

                booking.Property = property;
                return CreatedAtAction(nameof(GetById), new { id = booking.Id }, ToDto(booking));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating booking", error = ex.Message });
            }
        }

        [HttpGet("mine")]
        public async Task<ActionResult<List<BookingDto>>> GetMine()
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var bookings = await _unitOfWork.Bookings.GetByUserIdAsync(userId.Value);
                return Ok(bookings.Select(ToDto).ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving bookings", error = ex.Message });
            }
        }

        [HttpGet("received")]
        public async Task<ActionResult<List<BookingDto>>> GetReceived()
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var bookings = await _unitOfWork.Bookings.GetByAgentIdAsync(userId.Value);
                return Ok(bookings.Select(ToDto).ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving bookings", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BookingDto>> GetById(int id)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var booking = await _unitOfWork.Bookings.GetFirstOrDefaultAsync(b => b.Id == id);
                if (booking == null)
                {
                    return NotFound(new { message = "Booking not found" });
                }

                var property = await _unitOfWork.Properties.GetByIdAsync(booking.PropertyId);
                var isOwnerOrAgent = booking.UserId == userId.Value ||
                                      (property != null && property.UserId == userId.Value) ||
                                      User.IsInRole("Admin");

                if (!isOwnerOrAgent)
                {
                    return Forbid();
                }

                booking.Property = property!;
                return Ok(ToDto(booking));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving booking", error = ex.Message });
            }
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] BookingUpdateStatusDto dto)
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

                var booking = await _unitOfWork.Bookings.GetByIdAsync(id);
                if (booking == null)
                {
                    return NotFound(new { message = "Booking not found" });
                }

                var property = await _unitOfWork.Properties.GetByIdAsync(booking.PropertyId);
                var isAgentOrAdmin = (property != null && property.UserId == userId.Value) || User.IsInRole("Admin");
                var isRequestingUserCancelling = booking.UserId == userId.Value && dto.Status == BookingStatus.Cancelled;

                if (!isAgentOrAdmin && !isRequestingUserCancelling)
                {
                    return Forbid();
                }

                booking.Status = dto.Status;
                booking.RespondedAt = DateTime.UtcNow;
                if (dto.Status is BookingStatus.Cancelled or BookingStatus.Rejected)
                {
                    booking.CancellationReason = dto.CancellationReason;
                }

                _unitOfWork.Bookings.Update(booking);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { message = "Booking status updated", bookingId = id, status = booking.Status.ToString() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating booking", error = ex.Message });
            }
        }
    }
}
