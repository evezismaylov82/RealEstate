using RealEstateAPI.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateAPI.Application.DTOs.Property
{
    public class PropertyUpdateDto
    {
        [StringLength(200, MinimumLength = 10)]
        public string? Title { get; set; }

        [StringLength(5000, MinimumLength = 50)]
        public string? Description { get; set; }

        public PropertyType? Type { get; set; }
        public PropertyStatus? Status { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Price { get; set; }

        [StringLength(3)]
        public string? Currency { get; set; }

        [Range(1, 100000)]
        public decimal? Area { get; set; }

        [Range(0, 50)]
        public int? Bedrooms { get; set; }

        [Range(0, 50)]
        public int? Bathrooms { get; set; }

        [Range(0, 50)]
        public int? LivingRooms { get; set; }

        [Range(-5, 200)]
        public int? Floor { get; set; }

        [Range(1, 200)]
        public int? TotalFloors { get; set; }

        [Range(1800, 2100)]
        public int? BuildYear { get; set; }

        public bool? HasBalcony { get; set; }
        public bool? HasElevator { get; set; }
        public bool? HasParking { get; set; }
        public bool? IsFurnished { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? District { get; set; }

        [StringLength(100)]
        public string? Neighborhood { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [Url]
        public string? VideoUrl { get; set; }

        [Url]
        public string? VirtualTourUrl { get; set; }
    }
}
