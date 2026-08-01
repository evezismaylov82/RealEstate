using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging;
using RealEstateAPI.Application.DTOs.Property;
using RealEstateAPI.Domain.Entities;
using RealEstateAPI.Domain.Interfaces.Repositories;
using RealEstateAPI.Infrastructure.Services;
using System.Security.Claims;

namespace RealEstateAPI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PropertiesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IFileService _fileService;
        private readonly IOutputCacheStore _outputCacheStore;
        private readonly ILogger<PropertiesController> _logger;

        public PropertiesController(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IFileService fileService,
            IOutputCacheStore outputCacheStore,
            ILogger<PropertiesController> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _fileService = fileService;
            _outputCacheStore = outputCacheStore;
            _logger = logger;
        }

        [HttpGet]
        [OutputCache(PolicyName = "PropertiesCache")]
        public async Task<ActionResult<PaginatedResponse<PropertyDto>>> GetProperties(
            [FromQuery] PropertyFilterDto filter)
        {
            _logger.LogInformation("GetProperties request received with PageNumber: {PageNumber}, PageSize: {PageSize}, City: {City}.",
                filter?.PageNumber, filter?.PageSize, filter?.City);
            try
            {
                var (items, totalCount) = await _unitOfWork.Properties.GetPublishedPropertiesAsync(
                    pageNumber: filter.PageNumber,
                    pageSize: filter.PageSize,
                    city: filter.City,
                    type: filter.Type,
                    status: filter.Status,
                    minPrice: filter.MinPrice,
                    maxPrice: filter.MaxPrice
                );

                var propertyDtos = _mapper.Map<List<PropertyDto>>(items);

                var response = new PaginatedResponse<PropertyDto>
                {
                    Items = propertyDtos,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize
                };

                _logger.LogInformation("Successfully retrieved {Count} properties out of total {TotalCount}.", propertyDtos.Count, totalCount);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetProperties endpoint.");
                return StatusCode(500, new { message = "Error retrieving properties", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PropertyDto>> GetProperty(int id)
        {
            _logger.LogInformation("GetProperty request received for PropertyId: {PropertyId}.", id);
            try
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(id);

                if (property == null)
                {
                    _logger.LogWarning("GetProperty failed. Property with Id {PropertyId} was not found.", id);
                    return NotFound(new { message = "Property not found" });
                }

                await _unitOfWork.Properties.IncrementViewCountAsync(id);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Incremented view count and retrieved details for PropertyId: {PropertyId}.", id);
                var propertyDto = _mapper.Map<PropertyDto>(property);
                return Ok(propertyDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetProperty for PropertyId: {PropertyId}.", id);
                return StatusCode(500, new { message = "Error retrieving property", error = ex.Message });
            }
        }

        [HttpGet("featured")]
        [OutputCache(PolicyName = "PropertiesCache")]
        public async Task<ActionResult<List<PropertyDto>>> GetFeaturedProperties([FromQuery] int count = 10)
        {
            _logger.LogInformation("GetFeaturedProperties request received with Count: {Count}.", count);
            try
            {
                var properties = await _unitOfWork.Properties.GetFeaturedPropertiesAsync(count);
                var propertyDtos = _mapper.Map<List<PropertyDto>>(properties);

                _logger.LogInformation("Successfully retrieved {Count} featured properties.", propertyDtos.Count);
                return Ok(propertyDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetFeaturedProperties.");
                return StatusCode(500, new { message = "Error retrieving featured properties", error = ex.Message });
            }
        }

        [HttpGet("most-viewed")]
        [OutputCache(PolicyName = "PropertiesCache")]
        public async Task<ActionResult<List<PropertyDto>>> GetMostViewedProperties([FromQuery] int count = 10)
        {
            _logger.LogInformation("GetMostViewedProperties request received with Count: {Count}.", count);
            try
            {
                var properties = await _unitOfWork.Properties.GetMostViewedPropertiesAsync(count);
                var propertyDtos = _mapper.Map<List<PropertyDto>>(properties);

                _logger.LogInformation("Successfully retrieved {Count} most viewed properties.", propertyDtos.Count);
                return Ok(propertyDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetMostViewedProperties.");
                return StatusCode(500, new { message = "Error retrieving most viewed properties", error = ex.Message });
            }
        }

        [HttpGet("latest")]
        [OutputCache(PolicyName = "PropertiesCache")]
        public async Task<ActionResult<List<PropertyDto>>> GetLatestProperties([FromQuery] int count = 10)
        {
            _logger.LogInformation("GetLatestProperties request received with Count: {Count}.", count);
            try
            {
                var properties = await _unitOfWork.Properties.GetLatestPropertiesAsync(count);
                var propertyDtos = _mapper.Map<List<PropertyDto>>(properties);

                _logger.LogInformation("Successfully retrieved {Count} latest properties.", propertyDtos.Count);
                return Ok(propertyDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetLatestProperties.");
                return StatusCode(500, new { message = "Error retrieving latest properties", error = ex.Message });
            }
        }

        [HttpGet("{id}/similar")]
        [OutputCache(PolicyName = "PropertiesCache")]
        public async Task<ActionResult<List<PropertyDto>>> GetSimilarProperties(int id, [FromQuery] int count = 5)
        {
            _logger.LogInformation("GetSimilarProperties request received for PropertyId: {PropertyId} with Count: {Count}.", id, count);
            try
            {
                var properties = await _unitOfWork.Properties.GetSimilarPropertiesAsync(id, count);
                var propertyDtos = _mapper.Map<List<PropertyDto>>(properties);

                _logger.LogInformation("Successfully retrieved {Count} similar properties for PropertyId: {PropertyId}.", propertyDtos.Count, id);
                return Ok(propertyDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetSimilarProperties for PropertyId: {PropertyId}.", id);
                return StatusCode(500, new { message = "Error retrieving similar properties", error = ex.Message });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<PropertyDto>> CreateProperty([FromBody] PropertyCreateDto createDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("CreateProperty request received from UserId: {UserId}.", userIdClaim);
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("CreateProperty failed due to invalid ModelState.");
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("CreateProperty failed. Invalid token claim.");
                    return Unauthorized(new { message = "Invalid token" });
                }

                var property = _mapper.Map<Property>(createDto);
                property.UserId = userId;
                property.IsPublished = false;

                await _unitOfWork.Properties.AddAsync(property);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Property created with Id: {PropertyId} by UserId: {UserId}. Evicting 'properties' output cache tag.", property.Id, userId);
                await _outputCacheStore.EvictByTagAsync("properties", HttpContext.RequestAborted);

                var propertyDto = _mapper.Map<PropertyDto>(property);
                return CreatedAtAction(nameof(GetProperty), new { id = property.Id }, propertyDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating property.");
                return StatusCode(500, new { message = "Error creating property", error = ex.Message });
            }
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<ActionResult<PropertyDto>> UpdateProperty(int id, [FromBody] PropertyUpdateDto updateDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            _logger.LogInformation("UpdateProperty request received for PropertyId: {PropertyId} by UserId: {UserId}.", id, userIdClaim);

            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("UpdateProperty failed due to invalid ModelState for PropertyId: {PropertyId}.", id);
                    return BadRequest(ModelState);
                }

                var property = await _unitOfWork.Properties.GetByIdAsync(id);
                if (property == null)
                {
                    _logger.LogWarning("UpdateProperty failed. Property with Id {PropertyId} not found.", id);
                    return NotFound(new { message = "Property not found" });
                }

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("UpdateProperty failed. Invalid token claim.");
                    return Unauthorized(new { message = "Invalid token" });
                }

                if (property.UserId != userId && userRole != "Admin")
                {
                    _logger.LogWarning("UpdateProperty forbidden. UserId {UserId} cannot update PropertyId: {PropertyId}.", userId, id);
                    return Forbid();
                }

                _mapper.Map(updateDto, property);

                _unitOfWork.Properties.Update(property);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Property updated for PropertyId: {PropertyId}. Evicting 'properties' output cache tag.", id);
                await _outputCacheStore.EvictByTagAsync("properties", HttpContext.RequestAborted);

                var propertyDto = _mapper.Map<PropertyDto>(property);
                return Ok(propertyDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in UpdateProperty for PropertyId: {PropertyId}.", id);
                return StatusCode(500, new { message = "Error updating property", error = ex.Message });
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProperty(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            _logger.LogInformation("DeleteProperty request received for PropertyId: {PropertyId} by UserId: {UserId}.", id, userIdClaim);

            try
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(id);
                if (property == null)
                {
                    _logger.LogWarning("DeleteProperty failed. Property with Id {PropertyId} not found.", id);
                    return NotFound(new { message = "Property not found" });
                }

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("DeleteProperty failed. Invalid token claim.");
                    return Unauthorized(new { message = "Invalid token" });
                }

                if (property.UserId != userId && userRole != "Admin")
                {
                    _logger.LogWarning("DeleteProperty forbidden. UserId {UserId} cannot delete PropertyId: {PropertyId}.", userId, id);
                    return Forbid();
                }

                _unitOfWork.Properties.Delete(property);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Property deleted for PropertyId: {PropertyId}. Evicting 'properties' output cache tag.", id);
                await _outputCacheStore.EvictByTagAsync("properties", HttpContext.RequestAborted);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in DeleteProperty for PropertyId: {PropertyId}.", id);
                return StatusCode(500, new { message = "Error deleting property", error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("my-properties")]
        public async Task<ActionResult<List<PropertyDto>>> GetMyProperties()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("GetMyProperties request received for UserId: {UserId}.", userIdClaim);

            try
            {
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("GetMyProperties failed. Invalid token claim.");
                    return Unauthorized(new { message = "Invalid token" });
                }

                var properties = await _unitOfWork.Properties.GetPropertiesByUserIdAsync(userId);
                var propertyDtos = _mapper.Map<List<PropertyDto>>(properties);

                _logger.LogInformation("Successfully retrieved {Count} properties for UserId: {UserId}.", propertyDtos.Count, userId);
                return Ok(propertyDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetMyProperties for UserId: {UserId}.", userIdClaim);
                return StatusCode(500, new { message = "Error retrieving properties", error = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("{id}/upload-images")]
        public async Task<IActionResult> UploadImages(int id, [FromForm] List<IFormFile> files)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("UploadImages request received for PropertyId: {PropertyId} with {FileCount} files by UserId: {UserId}.",
                id, files?.Count ?? 0, userIdClaim);

            try
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(id);
                if (property == null)
                {
                    _logger.LogWarning("UploadImages failed. Property with Id {PropertyId} not found.", id);
                    return NotFound(new { message = "Property not found" });
                }

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("UploadImages failed. Invalid token claim.");
                    return Unauthorized(new { message = "Invalid token" });
                }

                if (property.UserId != userId)
                {
                    _logger.LogWarning("UploadImages forbidden. UserId {UserId} does not own PropertyId: {PropertyId}.", userId, id);
                    return Forbid();
                }

                var uploadedUrls = await _fileService.UploadMultipleImagesAsync(files, "properties");

                var displayOrder = await _unitOfWork.PropertyImages.CountAsync(i => i.PropertyId == id);

                foreach (var url in uploadedUrls)
                {
                    var image = new PropertyImage
                    {
                        PropertyId = id,
                        ImageUrl = url,
                        DisplayOrder = ++displayOrder,
                        IsCover = displayOrder == 1
                    };

                    await _unitOfWork.PropertyImages.AddAsync(image);
                }

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Uploaded {Count} images for PropertyId: {PropertyId}. Evicting 'properties' output cache tag.", uploadedUrls.Count, id);
                await _outputCacheStore.EvictByTagAsync("properties", HttpContext.RequestAborted);

                return Ok(new { message = "Images uploaded successfully", urls = uploadedUrls });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in UploadImages for PropertyId: {PropertyId}.", id);
                return StatusCode(500, new { message = "Error uploading images", error = ex.Message });
            }
        }
    }
}