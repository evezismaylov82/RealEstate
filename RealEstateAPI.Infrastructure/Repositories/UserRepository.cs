using Microsoft.EntityFrameworkCore;
using RealEstateAPI.Domain.Entities;
using RealEstateAPI.Domain.Enums;
using RealEstateAPI.Domain.Interfaces.Repositories;
using RealEstateAPI.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateAPI.Infrastructure.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {

        private static readonly Func<ApplicationDbContext, string, Task<User?>> _getByEmailCompiled =
            EF.CompileAsyncQuery((ApplicationDbContext ctx, string email) =>
                ctx.Users.FirstOrDefault(u => u.Email.ToLower() == email.ToLower()));

        public UserRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _getByEmailCompiled(_context, email.ToLower());
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _dbSet
                .FirstOrDefaultAsync(u => u.Username != null && u.Username.ToLower() == username.ToLower());
        }

        public async Task<bool> IsUsernameExistsAsync(string username)
        {
            return await _dbSet
                .AnyAsync(u => u.Username != null && u.Username.ToLower() == username.ToLower());
        }

        public async Task<User?> GetByEmailChangeTokenAsync(string token)
        {
            return await _dbSet
                .FirstOrDefaultAsync(u =>
                    u.EmailChangeToken == token &&
                    u.EmailChangeTokenExpiry > DateTime.UtcNow);
        }

        public async Task<User?> GetByEmailVerificationTokenAsync(string token)
        {
            return await _dbSet
                .FirstOrDefaultAsync(u => u.EmailVerificationToken == token);
        }

        public async Task<User?> GetByPasswordResetTokenAsync(string token)
        {
            return await _dbSet
                .FirstOrDefaultAsync(u =>
                    u.PasswordResetToken == token &&
                    u.PasswordResetTokenExpiry > DateTime.UtcNow);
        }

        public async Task<bool> IsEmailExistsAsync(string email)
        {
            return await _dbSet
                .AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role)
        {
            return await _dbSet
                .Where(u => u.Role == role)
                .OrderBy(u => u.FirstName)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetUnverifiedUsersAsync()
        {
            return await _dbSet
                .Where(u => !u.IsEmailVerified)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }
    }
}
