using System.Diagnostics;
using System.Text.Json;

namespace AuthorizationService.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception while processing request");
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                var payload = new
                {
                    title = "An unexpected error occurred.",
                    detail = ex.Message,
                    status = context.Response.StatusCode,
                    traceId = Activity.Current?.Id ?? context.TraceIdentifier
                };

                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var json = JsonSerializer.Serialize(payload, options);
                await context.Response.WriteAsync(json);
            }
        }
    }
}
