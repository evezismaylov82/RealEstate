using Microsoft.EntityFrameworkCore;
using RealEstateAPI.Domain.Entities;
using RealEstateAPI.Domain.Interfaces.Repositories;
using RealEstateAPI.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateAPI.Infrastructure.Repositories
{
    public class PropertyImageRepository : GenericRepository<PropertyImage>, IPropertyImageRepository
    {
        public PropertyImageRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<PropertyImage>> GetImagesByPropertyIdAsync(int propertyId)
        {
            return await _dbSet
                .Where(i => i.PropertyId == propertyId)
                .OrderBy(i => i.DisplayOrder)
                .ToListAsync();
        }

        public async Task<PropertyImage?> GetCoverImageByPropertyIdAsync(int propertyId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(i => i.PropertyId == propertyId && i.IsCover);
        }

        public async Task DeleteImagesByPropertyIdAsync(int propertyId)
        {
            var images = await _dbSet
                .Where(i => i.PropertyId == propertyId)
                .ToListAsync();

            DeleteRange(images);
        }
    }
}
