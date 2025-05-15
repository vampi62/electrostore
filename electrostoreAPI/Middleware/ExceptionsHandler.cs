using System.Net;
using System.Text.Json;

namespace electrostore.Middleware;

public class ExceptionsHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionsHandler> _logger;

    public ExceptionsHandler(RequestDelegate next, ILogger<ExceptionsHandler> logger)
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
            // ne pas logger les erreurs 4xx
            if (ex is ArgumentException || ex is KeyNotFoundException || ex is InvalidOperationException)
            {
                await HandleExceptionAsync(context, ex);
            }
            else
            {
                _logger.LogError(ex, "An error occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = exception switch
        {
            ArgumentException _ => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException _ => StatusCodes.Status401Unauthorized,
            KeyNotFoundException _ => StatusCodes.Status404NotFound,
            InvalidOperationException _ => StatusCodes.Status409Conflict,
            NotImplementedException _ => StatusCodes.Status501NotImplemented,
            _ => StatusCodes.Status500InternalServerError
        };

        var result = JsonSerializer.Serialize(new { error = exception.Message });
        return context.Response.WriteAsync(result);
    }
}