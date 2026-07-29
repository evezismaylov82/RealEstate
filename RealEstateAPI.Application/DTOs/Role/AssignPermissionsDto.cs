using System.Collections.Generic;

namespace RealEstateAPI.Application.DTOs.Role
{
    public class AssignPermissionsDto
    {

        public List<int> PermissionIds { get; set; } = new();
    }
}
