using RealEstateAPI.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RealEstateAPI.Domain.Interfaces.Repositories
{
    public interface IBookingRepository : IGenericRepository<Booking>
    {
        Task<IEnumerable<Booking>> GetByUserIdAsync(int userId);
        Task<IEnumerable<Booking>> GetByPropertyIdAsync(int propertyId);
        Task<IEnumerable<Booking>> GetByAgentIdAsync(int agentUserId);
        Task<bool> HasOverlappingBookingAsync(int propertyId, System.DateTime requestedDateTime);
    }
}
