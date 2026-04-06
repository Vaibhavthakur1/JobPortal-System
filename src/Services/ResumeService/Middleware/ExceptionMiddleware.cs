using System.Net;
using System.Text.Json;

namespace ResumeService.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (KeyNotFoundException ex) { await Write(context, HttpStatusCode.NotFound, ex.Message); }
        catch (UnauthorizedAccessException ex) { await Write(context, HttpStatusCode.Forbidden, ex.Message); }
        catch (InvalidOperationException ex) { await Write(context, HttpStatusCode.BadRequest, ex.Message); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await Write(context, HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task Write(HttpContext ctx, HttpStatusCode code, string message)
    {
        ctx.Response.StatusCode = (int)code;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { success = false, message }));
    }
}
