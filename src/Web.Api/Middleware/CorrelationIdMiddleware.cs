using Serilog.Context;

namespace Web.Api.Middleware;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private const string CorrelationIdHeaderName = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = GetOrCreateCorrelationId(context);

        context.Response.Headers.TryAdd(CorrelationIdHeaderName, correlationId);
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            _logger.LogDebug("Processing request {Method} {Path} with correlation ID {CorrelationId}",
                context.Request.Method, context.Request.Path, correlationId);

            await _next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out Microsoft.Extensions.Primitives.StringValues correlationId) &&
            !string.IsNullOrEmpty(correlationId))
        {
            return correlationId.ToString();
        }
        return Guid.NewGuid().ToString();
    }
}