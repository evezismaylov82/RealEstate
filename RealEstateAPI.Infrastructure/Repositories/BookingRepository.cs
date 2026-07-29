using Microsoft.EntityFrameworkCore;
using RealEstateAPI.Domain.Entities;
using RealEstateAPI.Domain.Enums;
using RealEstateAPI.Domain.Interfaces.Repositories;
using RealEstateAPI.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RealEstateAPI.Infrastructure.Repositories
{
    public class BookingRepository : GenericRepository<Booking>, IBookingRepository
    {
        public BookingRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Booking>> GetByUserIdAsync(int userId)
        {
            return await _dbSet
                .Include(b => b.Property)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.RequestedDateTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetByPropertyIdAsync(int propertyId)
        {
            return await _dbSet
                .Include(b => b.User)
                .Where(b => b.PropertyId == propertyId)
                .OrderByDescending(b => b.RequestedDateTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetByAgentIdAsync(int agentUserId)
        {
            return await _dbSet
                .Include(b => b.Property)
                .Include(b => b.User)
                .Where(b => b.Property.UserId == agentUserId)
                .OrderByDescending(b => b.RequestedDateTime)
                .ToListAsync();
        }

        public async Task<bool> HasOverlappingBookingAsync(int propertyId, DateTime requestedDateTime)
        {
            var windowStart = requestedDateTime.AddMinutes(-30);
            var windowEnd = requestedDateTime.AddMinutes(30);

            return await _dbSet.AnyAsync(b =>
                b.PropertyId == propertyId &&
                b.Status != BookingStatus.Cancelled &&
                b.Status != BookingStatus.Rejected &&
                b.RequestedDateTime >= windowStart &&
                b.RequestedDateTime <= windowEnd);
        }
    }
}
