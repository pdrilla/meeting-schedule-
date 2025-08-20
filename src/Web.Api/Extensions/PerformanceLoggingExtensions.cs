using Web.Api.Middleware;

namespace Web.Api.Extensions;

public static class PerformanceLoggingExtensions
{
    public static IApplicationBuilder UsePerformanceLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<PerformanceLoggingMiddleware>();
    }
}