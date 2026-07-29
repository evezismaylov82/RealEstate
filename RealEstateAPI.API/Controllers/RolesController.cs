using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealEstateAPI.API.Authorization;
using RealEstateAPI.Application.DTOs.Role;
using RealEstateAPI.Domain.Entities;
using RealEstateAPI.Domain.Interfaces.Repositories;

namespace RealEstateAPI.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public RolesController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        private static RoleDto ToDto(Role r) => new()
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            IsSystemRole = r.IsSystemRole,
            Permissions = r.RolePermissions?
                .Select(rp => new PermissionDto
                {
                    Id = rp.Permission.Id,
                    Name = rp.Permission.Name,
                    Description = rp.Permission.Description
                })
                .ToList() ?? new List<PermissionDto>()
        };

        [HttpGet]
        public async Task<ActionResult<List<RoleDto>>> GetAll()
        {
            try
            {
                var roles = await _unitOfWork.Roles.GetAllWithPermissionsAsync();
                return Ok(roles.Select(ToDto).ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving roles", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RoleDto>> GetById(int id)
        {
            try
            {
                var role = await _unitOfWork.Roles.GetWithPermissionsAsync(id);
                if (role == null)
                {
                    return NotFound(new { message = "Role not found" });
                }

                return Ok(ToDto(role));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving role", error = ex.Message });
            }
        }

        [HttpGet("permissions")]
        public async Task<ActionResult<List<PermissionDto>>> GetAllPermissions()
        {
            try
            {
                var permissions = await _unitOfWork.Permissions.GetAllAsync();
                var dtos = permissions
                    .Select(p => new PermissionDto { Id = p.Id, Name = p.Name, Description = p.Description })
                    .OrderBy(p => p.Name)
                    .ToList();

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving permissions", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<RoleDto>> Create([FromBody] RoleCreateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (await _unitOfWork.Roles.GetByNameAsync(dto.Name) != null)
                {
                    return BadRequest(new { message = "A role with this name already exists" });
                }

                var role = new Role
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    IsSystemRole = false
                };

                await _unitOfWork.Roles.AddAsync(role);
                await _unitOfWork.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = role.Id }, ToDto(role));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating role", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<RoleDto>> Update(int id, [FromBody] RoleUpdateDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var role = await _unitOfWork.Roles.GetByIdAsync(id);
                if (role == null)
                {
                    return NotFound(new { message = "Role not found" });
                }

                if (role.IsSystemRole)
                {
                    return BadRequest(new { message = "System roles cannot be modified" });
                }

                if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != role.Name)
                {
                    if (await _unitOfWork.Roles.GetByNameAsync(dto.Name) != null)
                    {
                        return BadRequest(new { message = "A role with this name already exists" });
                    }
                    role.Name = dto.Name;
                }

                if (dto.Description != null)
                {
                    role.Description = dto.Description;
                }

                _unitOfWork.Roles.Update(role);
                await _unitOfWork.SaveChangesAsync();

                return Ok(ToDto(role));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating role", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var role = await _unitOfWork.Roles.GetByIdAsync(id);
                if (role == null)
                {
                    return NotFound(new { message = "Role not found" });
                }

                if (role.IsSystemRole)
                {
                    return BadRequest(new { message = "System roles cannot be deleted" });
                }

                _unitOfWork.Roles.Delete(role);
                await _unitOfWork.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting role", error = ex.Message });
            }
        }

        [HttpPut("{id}/permissions")]
        public async Task<ActionResult<RoleDto>> AssignPermissions(int id, [FromBody] AssignPermissionsDto dto)
        {
            try
            {
                var role = await _unitOfWork.Roles.GetByIdAsync(id);
                if (role == null)
                {
                    return NotFound(new { message = "Role not found" });
                }

                await _unitOfWork.Roles.AssignPermissionsAsync(id, dto.PermissionIds);
                await _unitOfWork.SaveChangesAsync();

                var updated = await _unitOfWork.Roles.GetWithPermissionsAsync(id);
                return Ok(ToDto(updated!));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error assigning permissions", error = ex.Message });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<RoleDto>>> GetRolesForUser(int userId)
        {
            try
            {
                var roles = await _unitOfWork.Roles.GetRolesForUserAsync(userId);
                return Ok(roles.Select(ToDto).ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving user roles", error = ex.Message });
            }
        }

        [HttpPost("user/{userId}")]
        public async Task<IActionResult> AssignRoleToUser(int userId, [FromBody] AssignRoleToUserDto dto)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var role = await _unitOfWork.Roles.GetByIdAsync(dto.RoleId);
                if (role == null)
                {
                    return NotFound(new { message = "Role not found" });
                }

                await _unitOfWork.Roles.AssignRoleToUserAsync(userId, dto.RoleId);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { message = "Role assigned", userId, roleId = dto.RoleId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error assigning role", error = ex.Message });
            }
        }

        [HttpDelete("user/{userId}/{roleId}")]
        public async Task<IActionResult> RemoveRoleFromUser(int userId, int roleId)
        {
            try
            {
                await _unitOfWork.Roles.RemoveRoleFromUserAsync(userId, roleId);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { message = "Role removed", userId, roleId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error removing role", error = ex.Message });
            }
        }
    }
}
