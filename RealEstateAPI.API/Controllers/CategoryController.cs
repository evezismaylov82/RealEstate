using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
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

        public CategoryController(IUnitOfWork unitOfWork, IMapper mapper, IOutputCacheStore outputCacheStore)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _outputCacheStore = outputCacheStore;
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
            try
            {
                var categories = (await _unitOfWork.Categories.GetWithIncludesAsync(
                        null, nameof(Category.SubCategories), nameof(Category.Properties), nameof(Category.ParentCategory)))
                    .Where(c => c.ParentCategoryId == null)
                    .OrderBy(c => c.DisplayOrder)
                    .Select(ToDto)
                    .ToList();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving categories", error = ex.Message });
            }
        }

        [HttpGet("{slug}")]
        [OutputCache(PolicyName = "CategoriesCache")]
        public async Task<ActionResult<CategoryDto>> GetBySlug(string slug)
        {
            try
            {
                var category = await _unitOfWork.Categories.GetBySlugAsync(slug);
                if (category == null)
                {
                    return NotFound(new { message = "Category not found" });
                }

                return Ok(ToDto(category));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving category", error = ex.Message });
            }
        }

        [HasPermission("categories.manage")]
        [HttpPost]
        public async Task<ActionResult<CategoryDto>> Create([FromBody] CategoryCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var slug = Slugify(dto.Name);
                if (await _unitOfWork.Categories.SlugExistsAsync(slug))
                {
                    return BadRequest(new { message = "A category with this name already exists" });
                }

                if (dto.ParentCategoryId.HasValue &&
                    await _unitOfWork.Categories.GetByIdAsync(dto.ParentCategoryId.Value) == null)
                {
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
                await _outputCacheStore.EvictByTagAsync("categories", HttpContext.RequestAborted);

                return CreatedAtAction(nameof(GetBySlug), new { slug = category.Slug }, ToDto(category));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating category", error = ex.Message });
            }
        }

        [HasPermission("categories.manage")]
        [HttpPut("{id}")]
        public async Task<ActionResult<CategoryDto>> Update(int id, [FromBody] CategoryUpdateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var category = await _unitOfWork.Categories.GetByIdAsync(id);
                if (category == null)
                {
                    return NotFound(new { message = "Category not found" });
                }

                if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != category.Name)
                {
                    var newSlug = Slugify(dto.Name);
                    if (await _unitOfWork.Categories.SlugExistsAsync(newSlug, id))
                    {
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
                        return BadRequest(new { message = "A category cannot be its own parent" });
                    }
                    category.ParentCategoryId = dto.ParentCategoryId.Value;
                }

                _unitOfWork.Categories.Update(category);
                await _unitOfWork.SaveChangesAsync();
                await _outputCacheStore.EvictByTagAsync("categories", HttpContext.RequestAborted);

                return Ok(ToDto(category));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating category", error = ex.Message });
            }
        }

        [HasPermission("categories.manage")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var category = await _unitOfWork.Categories.GetByIdAsync(id);
                if (category == null)
                {
                    return NotFound(new { message = "Category not found" });
                }

                _unitOfWork.Categories.Delete(category);
                await _unitOfWork.SaveChangesAsync();
                await _outputCacheStore.EvictByTagAsync("categories", HttpContext.RequestAborted);

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting category", error = ex.Message });
            }
        }
    }
}
