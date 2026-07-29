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
    public class PropertyRepository : GenericRepository<Property>, IPropertyRepository
    {
        public PropertyRepository(ApplicationDbContext context) : base(context)
        {
        }

        public override async Task<Property?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Include(p => p.PropertyImages)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Property>> GetPropertiesByUserIdAsync(int userId)
        {
            return await _dbSet
                .Include(p => p.PropertyImages)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Property>> GetPropertiesByCityAsync(string city)
        {
            return await _dbSet
                .Include(p => p.PropertyImages)
                .Where(p => p.City.ToLower() == city.ToLower() && p.IsPublished)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Property>> GetPropertiesByTypeAndStatusAsync(
            PropertyType type,
            PropertyStatus status)
        {
            return await _dbSet
                .Include(p => p.PropertyImages)
                .Where(p => p.Type == type && p.Status == status && p.IsPublished)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Property>> GetPropertiesByPriceRangeAsync(
            decimal minPrice,
            decimal maxPrice,
            string? city = null)
        {
            var query = _dbSet
                .Include(p => p.PropertyImages)
                .Where(p => p.Price >= minPrice && p.Price <= maxPrice && p.IsPublished);

            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(p => p.City.ToLower() == city.ToLower());
            }

            return await query
                .OrderBy(p => p.Price)
                .ToListAsync();
        }

        public async Task<IEnumerable<Property>> GetFeaturedPropertiesAsync(int count = 10)
        {
            return await _dbSet
                .Include(p => p.PropertyImages)
                .Where(p => p.IsFeatured && p.IsPublished)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Property>> GetMostViewedPropertiesAsync(int count = 10)
        {
            return await _dbSet
                .Include(p => p.PropertyImages)
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.ViewCount)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Property>> GetLatestPropertiesAsync(int count = 10)
        {
            return await _dbSet
                .Include(p => p.PropertyImages)
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task IncrementViewCountAsync(int propertyId)
        {
            var property = await _dbSet.FindAsync(propertyId);
            if (property != null)
            {
                property.ViewCount++;
            }
        }

        public async Task<(IEnumerable<Property> Items, int TotalCount)> GetPublishedPropertiesAsync(
            int pageNumber,
            int pageSize,
            string? city = null,
            PropertyType? type = null,
            PropertyStatus? status = null,
            decimal? minPrice = null,
            decimal? maxPrice = null)
        {
            var query = _dbSet
                .Include(p => p.PropertyImages)
                .Where(p => p.IsPublished);

            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(p => p.City.ToLower() == city.ToLower());
            }

            if (type.HasValue)
            {
                query = query.Where(p => p.Type == type.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<IEnumerable<Property>> GetSimilarPropertiesAsync(
            int propertyId,
            int count = 5)
        {
            var property = await _dbSet.FindAsync(propertyId);
            if (property == null) return Enumerable.Empty<Property>();

            var minPrice = property.Price * 0.8m;
            var maxPrice = property.Price * 1.2m;

            return await _dbSet
                .Include(p => p.PropertyImages)
                .Where(p =>
                    p.Id != propertyId &&
                    p.City == property.City &&
                    p.Type == property.Type &&
                    p.Price >= minPrice &&
                    p.Price <= maxPrice &&
                    p.IsPublished)
                .OrderBy(p => Math.Abs(p.Price - property.Price))
                .Take(count)
                .ToListAsync();
        }
    }
}
