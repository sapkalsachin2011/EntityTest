using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using EntityTestApi.Exceptions;

namespace EntityTestApi.Middleware
{
    public class CustomExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<CustomExceptionHandler> _logger;
        private readonly IHostEnvironment _env;

        public CustomExceptionHandler(ILogger<CustomExceptionHandler> logger, IHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "[IExceptionHandler] Unhandled exception: {Message}", exception.Message);

            context.Response.StatusCode = exception switch
            {
                NotFoundException => (int)HttpStatusCode.NotFound,
                ValidationException => (int)HttpStatusCode.BadRequest,
                _ => (int)HttpStatusCode.InternalServerError
            };
            context.Response.ContentType = "application/json";

            var response = new
            {
                statusCode = context.Response.StatusCode,
                message = exception.Message,
                details = _env.IsDevelopment() ? exception.StackTrace : null,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            await context.Response.WriteAsJsonAsync(response, cancellationToken);
            return true; // Exception handled
        }
    }
}
