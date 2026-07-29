using Microsoft.EntityFrameworkCore;
using RealEstateAPI.Domain.Entities;
using RealEstateAPI.Domain.Interfaces.Repositories;
using RealEstateAPI.Infrastructure.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RealEstateAPI.Infrastructure.Repositories
{
    public class ReviewRepository : GenericRepository<Review>, IReviewRepository
    {
        public ReviewRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Review>> GetByPropertyIdAsync(int propertyId)
        {
            return await _dbSet
                .Include(r => r.ReviewerUser)
                .Where(r => r.PropertyId == propertyId && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Review>> GetByRevieweeUserIdAsync(int revieweeUserId)
        {
            return await _dbSet
                .Include(r => r.ReviewerUser)
                .Where(r => r.RevieweeUserId == revieweeUserId && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<double> GetAveragePropertyRatingAsync(int propertyId)
        {
            var query = _dbSet.Where(r => r.PropertyId == propertyId && r.IsApproved);
            return await query.AnyAsync() ? await query.AverageAsync(r => r.Rating) : 0;
        }

        public async Task<double> GetAverageUserRatingAsync(int revieweeUserId)
        {
            var query = _dbSet.Where(r => r.RevieweeUserId == revieweeUserId && r.IsApproved);
            return await query.AnyAsync() ? await query.AverageAsync(r => r.Rating) : 0;
        }

        public async Task<bool> HasUserReviewedPropertyAsync(int reviewerUserId, int propertyId)
        {
            return await _dbSet.AnyAsync(r => r.ReviewerUserId == reviewerUserId && r.PropertyId == propertyId);
        }

        public async Task<bool> HasUserReviewedUserAsync(int reviewerUserId, int revieweeUserId)
        {
            return await _dbSet.AnyAsync(r => r.ReviewerUserId == reviewerUserId && r.RevieweeUserId == revieweeUserId);
        }
    }
}
