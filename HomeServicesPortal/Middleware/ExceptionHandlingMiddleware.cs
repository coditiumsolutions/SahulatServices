using System.Net;
using System.Text.Json;
using HomeServicesPortal.Models.Api;

namespace HomeServicesPortal.Middleware;

/// <summary>Global exception handler for API routes — returns JSON error responses.</summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            if (!context.Request.Path.StartsWithSegments("/api"))
            {
                throw;
            }

            context.Response.ContentType = "application/json";

            var (statusCode, message) = ex switch
            {
                ArgumentException argEx => (HttpStatusCode.BadRequest, argEx.Message),
                InvalidOperationException opEx => (HttpStatusCode.BadRequest, opEx.Message),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized."),
                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
            };

            context.Response.StatusCode = (int)statusCode;
            var payload = ApiResponse<object>.Fail(message);
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
    }
}
