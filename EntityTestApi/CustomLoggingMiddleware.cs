namespace EntityTestApi.Middleware
{
    public class CustomLoggingMiddleware : IMiddleware
    {

        private readonly ILogger<CustomLoggingMiddleware> _logger;

        public CustomLoggingMiddleware(ILogger<CustomLoggingMiddleware> logger)
        {
            _logger = logger;   
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
             _logger.LogInformation($"→ Custom Middleware: Request {context.Request.Method} {context.Request.Path}");

            // Call the next middleware in the pipeline
            await next(context);

             _logger.LogInformation($"← Custom Middleware: Response {context.Response.StatusCode} for {context.Request.Method} {context.Request.Path}"); 
        }

        // Extension method for easy registration
   

    }
     public static class CustomLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CustomLoggingMiddleware>();
        }
    }
}