using System.Diagnostics;

namespace Web.Api.Middleware;

public class PerformanceLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceLoggingMiddleware> _logger;
    private const int SlowRequestThresholdMs = 1000;

    public PerformanceLoggingMiddleware(RequestDelegate next, ILogger<PerformanceLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        context.Response.OnStarting(() =>
        {
            stopwatch.Stop();

            long elapsedMs = stopwatch.ElapsedMilliseconds;
            string method = context.Request.Method;
            PathString path = context.Request.Path;
            int statusCode = context.Response.StatusCode;

            if (elapsedMs > SlowRequestThresholdMs)
            {
                _logger.LogWarning("Slow request detected: {Method} {Path} completed in {ElapsedMs}ms with status {StatusCode}",
                    method, path, elapsedMs, statusCode);
            }
            else
            {
                _logger.LogDebug("Request {Method} {Path} completed in {ElapsedMs}ms with status {StatusCode}",
                    method, path, elapsedMs, statusCode);
            }

            context.Response.Headers.TryAdd("X-Response-Time-Ms", elapsedMs.ToString(System.Globalization.CultureInfo.InvariantCulture));

            return Task.CompletedTask;
        });

        await _next(context);
    }
}