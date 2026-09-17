using Zeymera.Scoreboard.Api.Data;

namespace Zeymera.Scoreboard.Api.Middlewares;

public class ClientIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Client-Id";
    public const string ItemKey = "ClientId";

    private static readonly PathString _apiPath = "/api";
    private static readonly PathString _clientsPath = "/api/clients";

    public async Task InvokeAsync(HttpContext context, ScoreboardDbContext db)
    {
        if (!context.Request.Path.StartsWithSegments(_apiPath) || context.Request.Path.StartsWithSegments(_clientsPath))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var values) || string.IsNullOrWhiteSpace(values.ToString()))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = $"'{HeaderName}' header is required." });
            return;
        }

        var clientId = values.ToString();
        var client = await db.ClientSet.FindAsync(clientId);
        if (client is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Unknown client id." });
            return;
        }

        client.LastSeenAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        context.Items[ItemKey] = clientId;

        await next(context);
    }
}

public static class HttpContextClientIdExtensions
{
    public static string? GetClientId(this HttpContext context) =>
        context.Items.TryGetValue(ClientIdMiddleware.ItemKey, out var value) ? value as string : null;
}
