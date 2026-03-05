using System.Diagnostics;
using System.Globalization;

namespace Api.Middleware;

public sealed class RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        await next(context);
        stopwatch.Stop();

        if (!context.Response.HasStarted)
        {
            context.Response.Headers["X-Request-Duration-Ms"] = stopwatch.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture);
        }
        logger.LogInformation("{Method} {Path} took {Duration}ms", context.Request.Method, context.Request.Path, stopwatch.ElapsedMilliseconds);
    }
}
