using RealEstateAPI.Domain.Entities;
using RealEstateAPI.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateAPI.Domain.Interfaces.Repositories
{
     public interface IPropertyRepository : IGenericRepository<Property>
    {
      Task<IEnumerable<Property>> GetPropertiesByUserIdAsync(int userId);
        Task<IEnumerable<Property>> GetPropertiesByCityAsync(string city);
        Task<IEnumerable<Property>> GetPropertiesByTypeAndStatusAsync(PropertyType type, PropertyStatus status);
        Task<IEnumerable<Property>> GetPropertiesByPriceRangeAsync(decimal minPrice, decimal maxPrice, string? city = null);
        Task<IEnumerable<Property>> GetFeaturedPropertiesAsync(int count = 10);
        Task<IEnumerable<Property>> GetMostViewedPropertiesAsync(int count = 10);
        Task<IEnumerable<Property>> GetLatestPropertiesAsync(int count = 10);
        Task IncrementViewCountAsync(int propertyId);

        Task<(IEnumerable<Property> Items, int TotalCount)> GetPublishedPropertiesAsync(
            int pageNumber,
            int pageSize,
            string? city = null,
            PropertyType? type = null,
            PropertyStatus? status = null,
            decimal? minPrice = null,
            decimal? maxPrice = null);

        Task<IEnumerable<Property>> GetSimilarPropertiesAsync(int propertyId,int count = 5);
    }
}
