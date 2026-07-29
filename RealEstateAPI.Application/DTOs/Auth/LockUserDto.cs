using System;

namespace RealEstateAPI.Application.DTOs.Auth
{
    public class LockUserDto
    {

        public DateTime? LockedUntil { get; set; }
        public string? Reason { get; set; }
    }
}
