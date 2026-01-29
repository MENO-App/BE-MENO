using System.Net;
using System.Text.Json;
using FluentValidation;
using Application.Common.Exceptions;


namespace API.Middleware;

public sealed class GlobalExceptionMiddleware : IMiddleware
{
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(ILogger<GlobalExceptionMiddleware> logger)
        => _logger = logger;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex) // FluentValidation
        {
            _logger.LogWarning(ex, "Validation error");

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/problem+json";

            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            var problem = new
            {
                type = "https://httpstatuses.com/400",
                title = "Validation failed",
                status = 400,
                traceId = context.TraceIdentifier,
                errors
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
        catch (AppException ex)
        {
            _logger.LogInformation(ex, "Handled application exception");

            var (status, title, type) = ex switch
            {
                NotFoundException => (404, "Not Found", "https://httpstatuses.com/404"),
                UnauthorizedException => (401, "Unauthorized", "https://httpstatuses.com/401"),
                ForbiddenException => (403, "Forbidden", "https://httpstatuses.com/403"),
                BadRequestException => (400, "Bad Request", "https://httpstatuses.com/400"),
                _ => (400, "Bad Request", "https://httpstatuses.com/400")
            };

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";

            var problem = new
            {
                type,
                title,
                status,
                detail = ex.Message,
                traceId = context.TraceIdentifier
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var problem = new
            {
                type = "https://httpstatuses.com/500",
                title = "An unexpected error occurred",
                status = 500,
                traceId = context.TraceIdentifier
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
       

    }
}
