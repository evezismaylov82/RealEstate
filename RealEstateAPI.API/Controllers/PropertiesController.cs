using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
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

        public PropertiesController(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IFileService fileService,
            IOutputCacheStore outputCacheStore)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _fileService = fileService;
            _outputCacheStore = outputCacheStore;
        }

        [HttpGet]
        [OutputCache(PolicyName = "PropertiesCache")]
        public async Task<ActionResult<PaginatedResponse<PropertyDto>>> GetProperties(
            [FromQuery] PropertyFilterDto filter)
        {
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

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving properties", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PropertyDto>> GetProperty(int id)
        {
            try
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(id);

                if (property == null)
                {
                    return NotFound(new { message = "Property not found" });
                }

                await _unitOfWork.Properties.IncrementViewCountAsync(id);
                await _unitOfWork.SaveChangesAsync();

                var propertyDto = _mapper.Map<PropertyDto>(property);
                return Ok(propertyDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving property", error = ex.Message });
            }
        }

        [HttpGet("featured")]
        [OutputCache(PolicyName = "PropertiesCache")]
        public async Task<ActionResult<List<PropertyDto>>> GetFeaturedProperties([FromQuery] int count = 10)
        {
            try
            {
                var properties = await _unitOfWork.Properties.GetFeaturedPropertiesAsync(count);
                var propertyDtos = _mapper.Map<List<PropertyDto>>(properties);
                return Ok(propertyDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving featured properties", error = ex.Message });
            }
        }

        [HttpGet("most-viewed")]
        [OutputCache(PolicyName = "PropertiesCache")]
        public async Task<ActionResult<List<PropertyDto>>> GetMostViewedProperties([FromQuery] int count = 10)
        {
            try
            {
                var properties = await _unitOfWork.Properties.GetMostViewedPropertiesAsync(count);
                var propertyDtos = _mapper.Map<List<PropertyDto>>(properties);
                return Ok(propertyDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving most viewed properties", error = ex.Message });
            }
        }

        [HttpGet("latest")]
        [OutputCache(PolicyName = "PropertiesCache")]
        public async Task<ActionResult<List<PropertyDto>>> GetLatestProperties([FromQuery] int count = 10)
        {
            try
            {
                var properties = await _unitOfWork.Properties.GetLatestPropertiesAsync(count);
                var propertyDtos = _mapper.Map<List<PropertyDto>>(properties);
                return Ok(propertyDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving latest properties", error = ex.Message });
            }
        }

        [HttpGet("{id}/similar")]
        [OutputCache(PolicyName = "PropertiesCache")]
        public async Task<ActionResult<List<PropertyDto>>> GetSimilarProperties(int id, [FromQuery] int count = 5)
        {
            try
            {
                var properties = await _unitOfWork.Properties.GetSimilarPropertiesAsync(id, count);
                var propertyDtos = _mapper.Map<List<PropertyDto>>(properties);
                return Ok(propertyDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving similar properties", error = ex.Message });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<PropertyDto>> CreateProperty([FromBody] PropertyCreateDto createDto)
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
                    return Unauthorized(new { message = "Invalid token" });
                }

                var property = _mapper.Map<Property>(createDto);
                property.UserId = userId;
                property.IsPublished = false;

                await _unitOfWork.Properties.AddAsync(property);
                await _unitOfWork.SaveChangesAsync();
                await _outputCacheStore.EvictByTagAsync("properties", HttpContext.RequestAborted);

                var propertyDto = _mapper.Map<PropertyDto>(property);
                return CreatedAtAction(nameof(GetProperty), new { id = property.Id }, propertyDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating property", error = ex.Message });
            }
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<ActionResult<PropertyDto>> UpdateProperty(int id, [FromBody] PropertyUpdateDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var property = await _unitOfWork.Properties.GetByIdAsync(id);
                if (property == null)
                {
                    return NotFound(new { message = "Property not found" });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                if (property.UserId != userId && userRole != "Admin")
                {
                    return Forbid();
                }

                _mapper.Map(updateDto, property);

                _unitOfWork.Properties.Update(property);
                await _unitOfWork.SaveChangesAsync();
                await _outputCacheStore.EvictByTagAsync("properties", HttpContext.RequestAborted);

                var propertyDto = _mapper.Map<PropertyDto>(property);
                return Ok(propertyDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating property", error = ex.Message });
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProperty(int id)
        {
            try
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(id);
                if (property == null)
                {
                    return NotFound(new { message = "Property not found" });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                if (property.UserId != userId && userRole != "Admin")
                {
                    return Forbid();
                }

                _unitOfWork.Properties.Delete(property);
                await _unitOfWork.SaveChangesAsync();
                await _outputCacheStore.EvictByTagAsync("properties", HttpContext.RequestAborted);

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting property", error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("my-properties")]
        public async Task<ActionResult<List<PropertyDto>>> GetMyProperties()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var properties = await _unitOfWork.Properties.GetPropertiesByUserIdAsync(userId);
                var propertyDtos = _mapper.Map<List<PropertyDto>>(properties);
                return Ok(propertyDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving properties", error = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("{id}/upload-images")]
        public async Task<IActionResult> UploadImages(int id, [FromForm] List<IFormFile> files)
        {
            try
            {
                var property = await _unitOfWork.Properties.GetByIdAsync(id);
                if (property == null)
                {
                    return NotFound(new { message = "Property not found" });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                if (property.UserId != userId)
                {
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
                await _outputCacheStore.EvictByTagAsync("properties", HttpContext.RequestAborted);

                return Ok(new { message = "Images uploaded successfully", urls = uploadedUrls });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error uploading images", error = ex.Message });
            }
        }
    }
}
