using RealEstateAPI.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RealEstateAPI.Domain.Interfaces.Repositories
{
    public interface IRoleRepository : IGenericRepository<Role>
    {
        Task<Role?> GetByNameAsync(string name);
        Task<Role?> GetWithPermissionsAsync(int id);
        Task<List<Role>> GetAllWithPermissionsAsync();
        Task<List<string>> GetPermissionNamesForUserAsync(int userId);

        Task AssignPermissionsAsync(int roleId, IEnumerable<int> permissionIds);

        Task<List<Role>> GetRolesForUserAsync(int userId);
        Task AssignRoleToUserAsync(int userId, int roleId);
        Task RemoveRoleFromUserAsync(int userId, int roleId);
    }
}
