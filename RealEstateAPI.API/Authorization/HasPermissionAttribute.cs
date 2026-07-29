using Microsoft.AspNetCore.Authorization;

namespace RealEstateAPI.API.Authorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class HasPermissionAttribute : AuthorizeAttribute
    {
        public const string PolicyPrefix = "Permission:";

        public HasPermissionAttribute(string permission)
            : base(PolicyPrefix + permission)
        {
            Permission = permission;
        }

        public string Permission { get; }
    }
}
