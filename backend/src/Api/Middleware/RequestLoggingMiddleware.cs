namespace Api.Middleware;

public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public async Task Invoke(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault() ?? Guid.NewGuid().ToString("N");
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        logger.LogInformation("Handling {Method} {Path} CorrelationId={CorrelationId}", context.Request.Method, context.Request.Path, correlationId);
        await next(context);
        logger.LogInformation("Completed {Method} {Path} CorrelationId={CorrelationId} StatusCode={StatusCode}", context.Request.Method, context.Request.Path, correlationId, context.Response.StatusCode);
    }
}
