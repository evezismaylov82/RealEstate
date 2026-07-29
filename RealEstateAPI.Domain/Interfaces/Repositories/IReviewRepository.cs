using RealEstateAPI.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RealEstateAPI.Domain.Interfaces.Repositories
{
    public interface IReviewRepository : IGenericRepository<Review>
    {
        Task<IEnumerable<Review>> GetByPropertyIdAsync(int propertyId);
        Task<IEnumerable<Review>> GetByRevieweeUserIdAsync(int revieweeUserId);
        Task<double> GetAveragePropertyRatingAsync(int propertyId);
        Task<double> GetAverageUserRatingAsync(int revieweeUserId);
        Task<bool> HasUserReviewedPropertyAsync(int reviewerUserId, int propertyId);
        Task<bool> HasUserReviewedUserAsync(int reviewerUserId, int revieweeUserId);
    }
}
