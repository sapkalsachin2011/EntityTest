using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using EntityTestApi.Exceptions;

namespace EntityTestApi.Middleware
{
    /// <summary>
    /// Global exception handler middleware - catches and handles all unhandled exceptions
    /// </summary>
    public class GlobalExceptionHandlerMiddleware : IMiddleware
    {
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public GlobalExceptionHandlerMiddleware(
            ILogger<GlobalExceptionHandlerMiddleware> logger,
            IHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            
            var response = exception switch
            {
                // Business logic exceptions
                NotFoundException notFoundEx => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = notFoundEx.Message,
                    Details = _environment.IsDevelopment() ? notFoundEx.StackTrace : null
                },
                
                ValidationException validationEx => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = validationEx.Message,
                    Errors = validationEx.Errors,
                    Details = _environment.IsDevelopment() ? validationEx.StackTrace : null
                },
                
                UnauthorizedAccessException unauthorizedEx => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.Unauthorized,
                    Message = "Unauthorized access",
                    Details = _environment.IsDevelopment() ? unauthorizedEx.StackTrace : null
                },
                
                // Database exceptions
                DbUpdateConcurrencyException concurrencyEx => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.Conflict,
                    Message = "The record has been modified by another user. Please refresh and try again.",
                    Details = _environment.IsDevelopment() ? concurrencyEx.StackTrace : null
                },
                
                DbUpdateException dbUpdateEx => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = "Database operation failed. Please check your data.",
                    Details = _environment.IsDevelopment() ? dbUpdateEx.InnerException?.Message : null
                },
                
                // Timeout exceptions
                TimeoutException timeoutEx => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.RequestTimeout,
                    Message = "The request timed out. Please try again.",
                    Details = _environment.IsDevelopment() ? timeoutEx.StackTrace : null
                },
                
                // Generic exceptions
                _ => new ErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Message = _environment.IsDevelopment() 
                        ? exception.Message 
                        : "An error occurred while processing your request.",
                    Details = _environment.IsDevelopment() ? exception.StackTrace : null
                }
            };

            context.Response.StatusCode = response.StatusCode;

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(response, options);
            await context.Response.WriteAsync(json);
        }
    }

    /// <summary>
    /// Standardized error response model
    /// </summary>
    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }

    // /// <summary>
    // /// Extension method for easy middleware registration
    // /// </summary>
    // public static class GlobalExceptionHandlerMiddlewareExtensions
    // {
    //     public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    //     {
    //         return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    //     }
    // }
}
