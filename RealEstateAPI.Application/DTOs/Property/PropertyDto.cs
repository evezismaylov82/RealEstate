using RealEstateAPI.Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateAPI.Application.DTOs.Property
{
    public class PropertyDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal Area { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public int LivingRooms { get; set; }
        public int? Floor { get; set; }
        public int? TotalFloors { get; set; }
        public int? BuildYear { get; set; }
        public bool HasBalcony { get; set; }
        public bool HasElevator { get; set; }
        public bool HasParking { get; set; }
        public bool IsFurnished { get; set; }

        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string? Neighborhood { get; set; }
        public string Address { get; set; } = string.Empty;
        public string? PostalCode { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string FullLocation { get; set; } = string.Empty;

        public int ViewCount { get; set; }
        public bool IsFeatured { get; set; }
        public string? VideoUrl { get; set; }
        public string? VirtualTourUrl { get; set; }

        public UserDto? User { get; set; }
        public List<PropertyImageDto> Images { get; set; } = new();

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
