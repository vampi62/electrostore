using System.Diagnostics;

namespace ElectrostoreAPI.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        await _next(context);
        stopwatch.Stop();

        var statusCode = context.Response.StatusCode;
        if (statusCode >= 500)
        {
            _logger.LogWarning("HTTP {Method} {Path} -> {StatusCode} in {ElapsedMs}ms",
                context.Request.Method, context.Request.Path, statusCode, stopwatch.ElapsedMilliseconds);
        }
        else
        {
            _logger.LogInformation("HTTP {Method} {Path} -> {StatusCode} in {ElapsedMs}ms",
                context.Request.Method, context.Request.Path, statusCode, stopwatch.ElapsedMilliseconds);
        }
    }
}
