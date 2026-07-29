using Microsoft.EntityFrameworkCore;
using RealEstateAPI.Domain.Entities;
using RealEstateAPI.Domain.Interfaces.Repositories;
using RealEstateAPI.Infrastructure.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RealEstateAPI.Infrastructure.Repositories
{
    public class RoleRepository : GenericRepository<Role>, IRoleRepository
    {
        public RoleRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Role?> GetByNameAsync(string name)
        {
            return await _dbSet.FirstOrDefaultAsync(r => r.Name.ToLower() == name.ToLower());
        }

        public async Task<Role?> GetWithPermissionsAsync(int id)
        {
            return await _dbSet
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<List<Role>> GetAllWithPermissionsAsync()
        {
            return await _dbSet
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        public async Task<List<string>> GetPermissionNamesForUserAsync(int userId)
        {
            var permissionNames = await _context.Set<UserRoleAssignment>()
                .Where(ura => ura.UserId == userId)
                .SelectMany(ura => ura.Role.RolePermissions)
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToListAsync();

            return permissionNames;
        }

        public async Task AssignPermissionsAsync(int roleId, IEnumerable<int> permissionIds)
        {
            var existing = await _context.Set<RolePermission>()
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

            _context.Set<RolePermission>().RemoveRange(existing);

            var newAssignments = permissionIds
                .Distinct()
                .Select(permissionId => new RolePermission { RoleId = roleId, PermissionId = permissionId });

            await _context.Set<RolePermission>().AddRangeAsync(newAssignments);
        }

        public async Task<List<Role>> GetRolesForUserAsync(int userId)
        {
            return await _context.Set<UserRoleAssignment>()
                .Where(ura => ura.UserId == userId)
                .Select(ura => ura.Role)
                .ToListAsync();
        }

        public async Task AssignRoleToUserAsync(int userId, int roleId)
        {
            var alreadyAssigned = await _context.Set<UserRoleAssignment>()
                .AnyAsync(ura => ura.UserId == userId && ura.RoleId == roleId);

            if (alreadyAssigned)
            {
                return;
            }

            await _context.Set<UserRoleAssignment>().AddAsync(new UserRoleAssignment
            {
                UserId = userId,
                RoleId = roleId,
                AssignedAt = System.DateTime.UtcNow
            });
        }

        public async Task RemoveRoleFromUserAsync(int userId, int roleId)
        {
            var assignment = await _context.Set<UserRoleAssignment>()
                .FirstOrDefaultAsync(ura => ura.UserId == userId && ura.RoleId == roleId);

            if (assignment != null)
            {
                _context.Set<UserRoleAssignment>().Remove(assignment);
            }
        }
    }
}
