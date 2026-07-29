using RealEstateAPI.Domain.Entities;
using RealEstateAPI.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateAPI.Domain.Interfaces.Repositories
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByEmailVerificationTokenAsync(string token);
        Task<User?> GetByPasswordResetTokenAsync(string token);
        Task<bool> IsEmailExistsAsync(string email);
        Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role);
        Task<IEnumerable<User>> GetUnverifiedUsersAsync();
        Task<User?> GetByUsernameAsync(string username);
        Task<bool> IsUsernameExistsAsync(string username);
        Task<User?> GetByEmailChangeTokenAsync(string token);
    }
}
