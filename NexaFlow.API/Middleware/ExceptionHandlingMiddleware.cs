using NexaFlow.Application.Common.Exceptions;

namespace NexaFlow.API.Middleware;

/// <summary>
/// Translates unhandled exceptions into RFC-7807 ProblemDetails responses. Expected
/// <see cref="AppException"/>s map to their status; everything else becomes a 500. (T-014)
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var (status, type, title) = ex switch
        {
            ValidationException => (422, "validation_error", "Validation failed"),
            AppException appEx => (appEx.StatusCode, appEx.ErrorType, appEx.Message),
            _ => (500, "internal_error", "An unexpected error occurred.")
        };

        if (status == 500)
            logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);

        var problem = new
        {
            type = $"https://nexaflow.app/errors/{type}",
            title,
            status,
            traceId = context.TraceIdentifier,
            errors = (ex as ValidationException)?.Errors
        };

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    }
}
