using RealEstateAPI.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstateAPI.Application.DTOs.Property
{
    public class PropertyCreateDto
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, MinimumLength = 10, ErrorMessage = "Title must be between 10 and 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(5000, MinimumLength = 50, ErrorMessage = "Description must be between 50 and 5000 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Type is required")]
        public PropertyType Type { get; set; }

        [Required(ErrorMessage = "Status is required")]
        public PropertyStatus Status { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [StringLength(3)]
        public string Currency { get; set; } = "TRY";

        [Required(ErrorMessage = "Area is required")]
        [Range(1, 100000, ErrorMessage = "Area must be between 1 and 100000")]
        public decimal Area { get; set; }

        [Required(ErrorMessage = "Bedrooms is required")]
        [Range(0, 50, ErrorMessage = "Bedrooms must be between 0 and 50")]
        public int Bedrooms { get; set; }

        [Required(ErrorMessage = "Bathrooms is required")]
        [Range(0, 50, ErrorMessage = "Bathrooms must be between 0 and 50")]
        public int Bathrooms { get; set; }

        [Range(0, 50)]
        public int LivingRooms { get; set; } = 1;

        [Range(-5, 200)]
        public int? Floor { get; set; }

        [Range(1, 200)]
        public int? TotalFloors { get; set; }

        [Range(1800, 2100)]
        public int? BuildYear { get; set; }

        public bool HasBalcony { get; set; }
        public bool HasElevator { get; set; }
        public bool HasParking { get; set; }
        public bool IsFurnished { get; set; }

        [Required(ErrorMessage = "Country is required")]
        [StringLength(100)]
        public string Country { get; set; } = "Turkey";

        [Required(ErrorMessage = "City is required")]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "District is required")]
        [StringLength(100)]
        public string District { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Neighborhood { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        [StringLength(10)]
        public string? PostalCode { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        [Url(ErrorMessage = "Invalid video URL")]
        public string? VideoUrl { get; set; }

        [Url(ErrorMessage = "Invalid virtual tour URL")]
        public string? VirtualTourUrl { get; set; }
    }
}
