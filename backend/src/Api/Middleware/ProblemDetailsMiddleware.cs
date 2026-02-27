using System.Net;
using System.Text.Json;

namespace Api.Middleware;

public sealed class ProblemDetailsMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                title = "Unexpected server error",
                status = context.Response.StatusCode,
                detail = exception.Message
            }));
        }
    }
}
