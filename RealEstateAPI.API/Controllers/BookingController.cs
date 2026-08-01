using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<BookingController> _logger;

        public BookingController(IUnitOfWork unitOfWork, IMapper mapper, ILogger<BookingController> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
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
            var userId = GetUserId();
            _logger.LogInformation("Create booking request received for PropertyId: {PropertyId} by UserId: {UserId}.", dto?.PropertyId, userId);
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Create booking failed due to invalid ModelState for UserId: {UserId}.", userId);
                    return BadRequest(ModelState);
                }

                if (userId == null)
                {
                    _logger.LogWarning("Create booking failed. Invalid token claim.");
                    return Unauthorized(new { message = "Invalid token" });
                }

                if (dto.RequestedDateTime <= DateTime.UtcNow)
                {
                    _logger.LogWarning("Create booking failed. RequestedDateTime {RequestedTime} is not in the future.", dto.RequestedDateTime);
                    return BadRequest(new { message = "Requested date and time must be in the future" });
                }

                var property = await _unitOfWork.Properties.GetByIdAsync(dto.PropertyId);
                if (property == null)
                {
                    _logger.LogWarning("Create booking failed. Property with Id {PropertyId} not found.", dto.PropertyId);
                    return NotFound(new { message = "Property not found" });
                }

                if (await _unitOfWork.Bookings.HasOverlappingBookingAsync(dto.PropertyId, dto.RequestedDateTime))
                {
                    _logger.LogWarning("Create booking failed. Overlapping booking exists for PropertyId: {PropertyId} at {Time}.", dto.PropertyId, dto.RequestedDateTime);
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
                _logger.LogInformation("Successfully created booking with Id {BookingId} for PropertyId: {PropertyId}.", booking.Id, dto.PropertyId);
                return CreatedAtAction(nameof(GetById), new { id = booking.Id }, ToDto(booking));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating booking for PropertyId: {PropertyId}.", dto?.PropertyId);
                return StatusCode(500, new { message = "Error creating booking", error = ex.Message });
            }
        }

        [HttpGet("mine")]
        public async Task<ActionResult<List<BookingDto>>> GetMine()
        {
            var userId = GetUserId();
            _logger.LogInformation("GetMine bookings request received for UserId: {UserId}.", userId);
            try
            {
                if (userId == null)
                {
                    _logger.LogWarning("GetMine failed. Invalid token claim.");
                    return Unauthorized(new { message = "Invalid token" });
                }

                var bookings = await _unitOfWork.Bookings.GetByUserIdAsync(userId.Value);
                _logger.LogInformation("Successfully retrieved {Count} bookings for UserId: {UserId}.", bookings.Count(), userId.Value);
                return Ok(bookings.Select(ToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetMine for UserId: {UserId}.", userId);
                return StatusCode(500, new { message = "Error retrieving bookings", error = ex.Message });
            }
        }

        [HttpGet("received")]
        public async Task<ActionResult<List<BookingDto>>> GetReceived()
        {
            var userId = GetUserId();
            _logger.LogInformation("GetReceived bookings request received for Agent/User Id: {UserId}.", userId);
            try
            {
                if (userId == null)
                {
                    _logger.LogWarning("GetReceived failed. Invalid token claim.");
                    return Unauthorized(new { message = "Invalid token" });
                }

                var bookings = await _unitOfWork.Bookings.GetByAgentIdAsync(userId.Value);
                _logger.LogInformation("Successfully retrieved {Count} received bookings for AgentId: {UserId}.", bookings.Count(), userId.Value);
                return Ok(bookings.Select(ToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetReceived for AgentId: {UserId}.", userId);
                return StatusCode(500, new { message = "Error retrieving bookings", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BookingDto>> GetById(int id)
        {
            var userId = GetUserId();
            _logger.LogInformation("GetById booking request received for BookingId: {BookingId} by UserId: {UserId}.", id, userId);
            try
            {
                if (userId == null)
                {
                    _logger.LogWarning("GetById booking failed. Invalid token claim.");
                    return Unauthorized(new { message = "Invalid token" });
                }

                var booking = await _unitOfWork.Bookings.GetFirstOrDefaultAsync(b => b.Id == id);
                if (booking == null)
                {
                    _logger.LogWarning("GetById booking failed. Booking with Id {BookingId} not found.", id);
                    return NotFound(new { message = "Booking not found" });
                }

                var property = await _unitOfWork.Properties.GetByIdAsync(booking.PropertyId);
                var isOwnerOrAgent = booking.UserId == userId.Value ||
                                      (property != null && property.UserId == userId.Value) ||
                                      User.IsInRole("Admin");

                if (!isOwnerOrAgent)
                {
                    _logger.LogWarning("GetById booking forbidden. UserId {UserId} lacks permission for BookingId: {BookingId}.", userId.Value, id);
                    return Forbid();
                }

                booking.Property = property!;
                _logger.LogInformation("Successfully retrieved booking details for BookingId: {BookingId}.", id);
                return Ok(ToDto(booking));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetById booking for BookingId: {BookingId}.", id);
                return StatusCode(500, new { message = "Error retrieving booking", error = ex.Message });
            }
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] BookingUpdateStatusDto dto)
        {
            var userId = GetUserId();
            _logger.LogInformation("UpdateStatus booking request received for BookingId: {BookingId} to Status: {Status} by UserId: {UserId}.", id, dto?.Status, userId);
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("UpdateStatus booking failed due to invalid ModelState for BookingId: {BookingId}.", id);
                    return BadRequest(ModelState);
                }

                if (userId == null)
                {
                    _logger.LogWarning("UpdateStatus booking failed. Invalid token claim.");
                    return Unauthorized(new { message = "Invalid token" });
                }

                var booking = await _unitOfWork.Bookings.GetByIdAsync(id);
                if (booking == null)
                {
                    _logger.LogWarning("UpdateStatus booking failed. Booking with Id {BookingId} not found.", id);
                    return NotFound(new { message = "Booking not found" });
                }

                var property = await _unitOfWork.Properties.GetByIdAsync(booking.PropertyId);
                var isAgentOrAdmin = (property != null && property.UserId == userId.Value) || User.IsInRole("Admin");
                var isRequestingUserCancelling = booking.UserId == userId.Value && dto.Status == BookingStatus.Cancelled;

                if (!isAgentOrAdmin && !isRequestingUserCancelling)
                {
                    _logger.LogWarning("UpdateStatus booking forbidden. UserId {UserId} cannot change status of BookingId: {BookingId} to {Status}.", userId.Value, id, dto.Status);
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

                _logger.LogInformation("Successfully updated status of BookingId: {BookingId} to {Status}.", id, booking.Status);
                return Ok(new { message = "Booking status updated", bookingId = id, status = booking.Status.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in UpdateStatus for BookingId: {BookingId}.", id);
                return StatusCode(500, new { message = "Error updating booking", error = ex.Message });
            }
        }
    }
}