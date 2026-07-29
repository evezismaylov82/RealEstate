namespace RealEstateAPI.API.Middleware
{

    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _env;

        public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment env)
        {
            _next = next;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;

                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["Permissions-Policy"] =
                    "camera=(), microphone=(), geolocation=(), payment=(), usb=()";

                headers["X-XSS-Protection"] = "0";

                if (context.Request.IsHttps)
                {
                    headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
                }

                if (!context.Request.Path.StartsWithSegments("/swagger"))
                {
                    headers["Content-Security-Policy"] =
                        "default-src 'self'; img-src 'self' data: https:; " +
                        "script-src 'self'; style-src 'self'; " +
                        "font-src 'self' data:; connect-src 'self'; " +
                        "frame-ancestors 'none'; object-src 'none'";
                }

                headers.Remove("Server");
                headers.Remove("X-Powered-By");

                return Task.CompletedTask;
            });

            await _next(context);
        }
    }

    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            return app.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}
