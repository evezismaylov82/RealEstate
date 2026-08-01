using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging;
using RealEstateAPI.API.Authorization;
using RealEstateAPI.Application.DTOs.Category;
using RealEstateAPI.Domain.Entities;
using RealEstateAPI.Domain.Interfaces.Repositories;

namespace RealEstateAPI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IOutputCacheStore _outputCacheStore;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IOutputCacheStore outputCacheStore,
            ILogger<CategoryController> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _outputCacheStore = outputCacheStore;
            _logger = logger;
        }

        private static string Slugify(string name)
        {
            return name.Trim().ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("ə", "e").Replace("ı", "i").Replace("ö", "o")
                .Replace("ü", "u").Replace("ş", "s").Replace("ç", "c");
        }

        private CategoryDto ToDto(Category c) => new()
        {
            Id = c.Id,
            Name = c.Name,
            Slug = c.Slug,
            Description = c.Description,
            IconUrl = c.IconUrl,
            DisplayOrder = c.DisplayOrder,
            IsActive = c.IsActive,
            ParentCategoryId = c.ParentCategoryId,
            ParentCategoryName = c.ParentCategory?.Name,
            PropertyCount = c.Properties?.Count ?? 0,
            SubCategories = c.SubCategories?.Select(ToDto).ToList() ?? new List<CategoryDto>()
        };

        [HttpGet]
        [OutputCache(PolicyName = "CategoriesCache")]
        public async Task<ActionResult<List<CategoryDto>>> GetAll()
        {
            _logger.LogInformation("GetAll categories request received.");
            try
            {
                var categories = (await _unitOfWork.Categories.GetWithIncludesAsync(
                        null, nameof(Category.SubCategories), nameof(Category.Properties), nameof(Category.ParentCategory)))
                    .Where(c => c.ParentCategoryId == null)
                    .OrderBy(c => c.DisplayOrder)
                    .Select(ToDto)
                    .ToList();

                _logger.LogInformation("Successfully retrieved {Count} root categories.", categories.Count);
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetAll categories endpoint.");
                return StatusCode(500, new { message = "Error retrieving categories", error = ex.Message });
            }
        }

        [HttpGet("{slug}")]
        [OutputCache(PolicyName = "CategoriesCache")]
        public async Task<ActionResult<CategoryDto>> GetBySlug(string slug)
        {
            _logger.LogInformation("GetBySlug category request received for Slug: {Slug}.", slug);
            try
            {
                var category = await _unitOfWork.Categories.GetBySlugAsync(slug);
                if (category == null)
                {
                    _logger.LogWarning("GetBySlug category failed. Category with Slug '{Slug}' was not found.", slug);
                    return NotFound(new { message = "Category not found" });
                }

                _logger.LogInformation("Successfully retrieved category for Slug: {Slug}.", slug);
                return Ok(ToDto(category));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetBySlug category for Slug: {Slug}.", slug);
                return StatusCode(500, new { message = "Error retrieving category", error = ex.Message });
            }
        }

        [HasPermission("categories.manage")]
        [HttpPost]
        public async Task<ActionResult<CategoryDto>> Create([FromBody] CategoryCreateDto dto)
        {
            _logger.LogInformation("Create category request received for Name: {Name}.", dto?.Name);
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Create category failed due to invalid ModelState for Name: {Name}.", dto?.Name);
                    return BadRequest(ModelState);
                }

                var slug = Slugify(dto.Name);
                if (await _unitOfWork.Categories.SlugExistsAsync(slug))
                {
                    _logger.LogWarning("Create category failed. Category slug '{Slug}' already exists.", slug);
                    return BadRequest(new { message = "A category with this name already exists" });
                }

                if (dto.ParentCategoryId.HasValue &&
                    await _unitOfWork.Categories.GetByIdAsync(dto.ParentCategoryId.Value) == null)
                {
                    _logger.LogWarning("Create category failed. Parent CategoryId {ParentCategoryId} not found.", dto.ParentCategoryId.Value);
                    return BadRequest(new { message = "Parent category not found" });
                }

                var category = new Category
                {
                    Name = dto.Name,
                    Slug = slug,
                    Description = dto.Description,
                    IconUrl = dto.IconUrl,
                    DisplayOrder = dto.DisplayOrder,
                    IsActive = dto.IsActive,
                    ParentCategoryId = dto.ParentCategoryId
                };

                await _unitOfWork.Categories.AddAsync(category);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Category created with Id: {CategoryId}. Evicting 'categories' output cache tag.", category.Id);
                await _outputCacheStore.EvictByTagAsync("categories", HttpContext.RequestAborted);

                return CreatedAtAction(nameof(GetBySlug), new { slug = category.Slug }, ToDto(category));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating category for Name: {Name}.", dto?.Name);
                return StatusCode(500, new { message = "Error creating category", error = ex.Message });
            }
        }

        [HasPermission("categories.manage")]
        [HttpPut("{id}")]
        public async Task<ActionResult<CategoryDto>> Update(int id, [FromBody] CategoryUpdateDto dto)
        {
            _logger.LogInformation("Update category request received for CategoryId: {CategoryId}.", id);
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Update category failed due to invalid ModelState for CategoryId: {CategoryId}.", id);
                    return BadRequest(ModelState);
                }

                var category = await _unitOfWork.Categories.GetByIdAsync(id);
                if (category == null)
                {
                    _logger.LogWarning("Update category failed. Category with Id {CategoryId} not found.", id);
                    return NotFound(new { message = "Category not found" });
                }

                if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != category.Name)
                {
                    var newSlug = Slugify(dto.Name);
                    if (await _unitOfWork.Categories.SlugExistsAsync(newSlug, id))
                    {
                        _logger.LogWarning("Update category failed. New slug '{Slug}' already exists for another category.", newSlug);
                        return BadRequest(new { message = "A category with this name already exists" });
                    }
                    category.Name = dto.Name;
                    category.Slug = newSlug;
                }

                if (dto.Description != null) category.Description = dto.Description;
                if (dto.IconUrl != null) category.IconUrl = dto.IconUrl;
                if (dto.DisplayOrder.HasValue) category.DisplayOrder = dto.DisplayOrder.Value;
                if (dto.IsActive.HasValue) category.IsActive = dto.IsActive.Value;

                if (dto.ParentCategoryId.HasValue)
                {
                    if (dto.ParentCategoryId.Value == id)
                    {
                        _logger.LogWarning("Update category failed. CategoryId {CategoryId} attempted to set itself as parent.", id);
                        return BadRequest(new { message = "A category cannot be its own parent" });
                    }
                    category.ParentCategoryId = dto.ParentCategoryId.Value;
                }

                _unitOfWork.Categories.Update(category);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Category updated for CategoryId: {CategoryId}. Evicting 'categories' output cache tag.", id);
                await _outputCacheStore.EvictByTagAsync("categories", HttpContext.RequestAborted);

                return Ok(ToDto(category));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating category for CategoryId: {CategoryId}.", id);
                return StatusCode(500, new { message = "Error updating category", error = ex.Message });
            }
        }

        [HasPermission("categories.manage")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Delete category request received for CategoryId: {CategoryId}.", id);
            try
            {
                var category = await _unitOfWork.Categories.GetByIdAsync(id);
                if (category == null)
                {
                    _logger.LogWarning("Delete category failed. Category with Id {CategoryId} not found.", id);
                    return NotFound(new { message = "Category not found" });
                }

                _unitOfWork.Categories.Delete(category);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Category deleted for CategoryId: {CategoryId}. Evicting 'categories' output cache tag.", id);
                await _outputCacheStore.EvictByTagAsync("categories", HttpContext.RequestAborted);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting category for CategoryId: {CategoryId}.", id);
                return StatusCode(500, new { message = "Error deleting category", error = ex.Message });
            }
        }
    }
}