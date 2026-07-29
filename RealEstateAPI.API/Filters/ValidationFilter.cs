using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RealEstateAPI.API.Filters
{

    public class ValidationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var allErrors = new Dictionary<string, List<string>>();

            foreach (var (argumentName, argumentValue) in context.ActionArguments)
            {
                if (argumentValue is null)
                    continue;

                var argumentType = argumentValue.GetType();
                var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);

                if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                    continue;

                var validationContext = new ValidationContext<object>(argumentValue);
                var result = await validator.ValidateAsync(validationContext);

                if (result.IsValid)
                    continue;

                foreach (var failure in result.Errors)
                {
                    var key = string.IsNullOrEmpty(failure.PropertyName) ? argumentName : failure.PropertyName;

                    if (!allErrors.TryGetValue(key, out var messages))
                    {
                        messages = new List<string>();
                        allErrors[key] = messages;
                    }

                    if (!messages.Contains(failure.ErrorMessage))
                        messages.Add(failure.ErrorMessage);
                }
            }

            if (allErrors.Count > 0)
            {
                var errorResponse = new
                {
                    success = false,
                    message = "Validation failed",
                    statusCode = StatusCodes.Status400BadRequest,
                    errors = allErrors.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()),
                    timestamp = DateTime.UtcNow
                };

                context.Result = new BadRequestObjectResult(errorResponse);
                return;
            }

            await next();
        }
    }
}
