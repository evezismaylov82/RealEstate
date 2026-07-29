using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace RealEstateAPI.API.Middleware
{
    public class GlobalExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IWebHostEnvironment _env;

        public GlobalExceptionHandler(
            RequestDelegate next,
            ILogger<GlobalExceptionHandler> logger,
            IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var statusCode = HttpStatusCode.InternalServerError;
            var message = "An error occurred while processing your request";
            var errors = new List<string>();
            var logAsWarning = false;

            switch (exception)
            {
                case ValidationException validationEx:
                    statusCode = HttpStatusCode.BadRequest;
                    message = "Validation failed";
                    errors.AddRange(validationEx.Errors.Select(e => e.ErrorMessage));
                    logAsWarning = true;
                    break;

                case ArgumentNullException argNullEx:
                    statusCode = HttpStatusCode.BadRequest;
                    message = "Invalid input";
                    errors.Add(argNullEx.Message);
                    logAsWarning = true;
                    break;

                case ArgumentException argEx:
                    statusCode = HttpStatusCode.BadRequest;
                    message = "Invalid argument";
                    errors.Add(argEx.Message);
                    logAsWarning = true;
                    break;

                case UnauthorizedAccessException:
                    statusCode = HttpStatusCode.Unauthorized;
                    message = "Unauthorized access";
                    logAsWarning = true;
                    break;

                case KeyNotFoundException:
                    statusCode = HttpStatusCode.NotFound;
                    message = "Resource not found";
                    logAsWarning = true;
                    break;

                case DbUpdateConcurrencyException:
                    statusCode = HttpStatusCode.Conflict;
                    message = "The record was modified or deleted by another request. Please reload and try again.";
                    break;

                case DbUpdateException dbUpdateEx:
                    (statusCode, message, errors) = MapDbUpdateException(dbUpdateEx);
                    break;

                case OperationCanceledException or TaskCanceledException:

                    statusCode = (HttpStatusCode)499;
                    message = "The request was cancelled.";
                    logAsWarning = true;
                    break;

                case TimeoutException:
                    statusCode = HttpStatusCode.GatewayTimeout;
                    message = "The request timed out while processing.";
                    break;

                default:

                    errors.Add(_env.IsDevelopment()
                        ? exception.Message
                        : "An unexpected error occurred");
                    break;
            }

            if (logAsWarning)
            {
                _logger.LogWarning(exception, "Handled exception with status {StatusCode}: {Message}", (int)statusCode, message);
            }
            else
            {
                _logger.LogError(exception, "Unhandled exception with status {StatusCode}: {Message}", (int)statusCode, message);
            }

            context.Response.StatusCode = (int)statusCode;

            var errorResponse = new ErrorResponse
            {
                Success = false,
                Message = message,
                StatusCode = (int)statusCode,
                Errors = errors,
                Timestamp = DateTime.UtcNow
            };

            if (_env.IsDevelopment())
            {
                errorResponse.StackTrace = exception.StackTrace;
                errorResponse.InnerException = exception.InnerException?.Message;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(errorResponse, options);
            await context.Response.WriteAsync(json);
        }

        private (HttpStatusCode StatusCode, string Message, List<string> Errors) MapDbUpdateException(DbUpdateException exception)
        {
            if (exception.InnerException is SqlException sqlEx)
            {
                switch (sqlEx.Number)
                {
                    case 2601:
                    case 2627:
                        return (HttpStatusCode.Conflict,
                            "A record with the same unique value already exists.",
                            new List<string> { _env.IsDevelopment() ? sqlEx.Message : "Duplicate value violates a unique constraint." });

                    case 547:
                        return (HttpStatusCode.Conflict,
                            "This operation violates a data relationship (e.g. related records still exist, or a referenced record is missing).",
                            new List<string> { _env.IsDevelopment() ? sqlEx.Message : "Constraint violation." });

                    case -2:
                        return (HttpStatusCode.GatewayTimeout,
                            "The database operation timed out.",
                            new List<string>());
                }
            }

            return (HttpStatusCode.BadRequest,
                "The requested data change could not be saved.",
                new List<string> { _env.IsDevelopment() ? exception.Message : "Database update failed." });
        }
    }

    public class ErrorResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime Timestamp { get; set; }
        public string? StackTrace { get; set; }
        public string? InnerException { get; set; }
    }

    public static class GlobalExceptionHandlerExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GlobalExceptionHandler>();
        }
    }
}
