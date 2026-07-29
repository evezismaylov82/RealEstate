using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RealEstateAPI.API.Filters
{

    public class CsrfValidationFilter : IAsyncActionFilter
    {
        private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            HttpMethods.Get, HttpMethods.Head, HttpMethods.Options, HttpMethods.Trace
        };

        private readonly IAntiforgery _antiforgery;
        private readonly IConfiguration _configuration;

        public CsrfValidationFilter(IAntiforgery antiforgery, IConfiguration configuration)
        {
            _antiforgery = antiforgery;
            _configuration = configuration;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var request = context.HttpContext.Request;

            if (SafeMethods.Contains(request.Method))
            {
                await next();
                return;
            }

            if (request.Headers.ContainsKey("Authorization"))
            {

                await next();
                return;
            }

            var cookieName = _configuration["Antiforgery:CookieName"] ?? "XSRF-REQUEST-TOKEN";

            if (!request.Cookies.ContainsKey(cookieName))
            {

                await next();
                return;
            }

            try
            {
                await _antiforgery.ValidateRequestAsync(context.HttpContext);
            }
            catch (AntiforgeryValidationException)
            {
                context.Result = new ObjectResult(new
                {
                    success = false,
                    message = "CSRF validation failed. Missing or invalid anti-forgery token.",
                    statusCode = StatusCodes.Status403Forbidden,
                    timestamp = DateTime.UtcNow
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            await next();
        }
    }
}
