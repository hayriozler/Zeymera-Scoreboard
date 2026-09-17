using Microsoft.EntityFrameworkCore;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Zeymera.Scoreboard.Client.Components;
using Zeymera.Scoreboard.Client.Models;
using Zeymera.Scoreboard.Client.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Db");
if (!Directory.Exists(folder))
    Directory.CreateDirectory(folder);
var dbName = Path.Combine(folder, "scoreboard.db");

string playerPhotosFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Players");
if (!Directory.Exists(playerPhotosFolder))
    Directory.CreateDirectory(playerPhotosFolder);
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7153/";
builder.Services.AddDbContextFactory<DataContext>(options => options.UseSqlite($"Data Source={dbName}"));
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddScoped<LocalizationService>();
builder.Services.AddSingleton<WebSocketService>();
builder.Services.AddSingleton<ScoreboardCommandHub>();

var app = builder.Build();
app.UseAntiforgery();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DataContext>();
    db.Database.EnsureCreated();

    // EnsureCreated() is a no-op once the database file already exists, so it won't add
    // tables introduced after the first run - create any such tables here explicitly.
    db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS player (
            Id INTEGER PRIMARY KEY,
            Nickname TEXT NOT NULL,
            PhotoPath TEXT NULL
        );
        """);
}
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

var controlMessageJsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter() }
};

app.UseWebSockets();
app.Map("/ws/control", async (HttpContext context, ScoreboardCommandHub hub) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    hub.ControllerConnected();
    try
    {
        var buffer = new byte[1024];
        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), context.RequestAborted);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                break;
            }

            var text = Encoding.UTF8.GetString(buffer, 0, result.Count).Trim();
            ScoreboardCommandMessage? message;
            try
            {
                message = JsonSerializer.Deserialize<ScoreboardCommandMessage>(text, controlMessageJsonOptions);
            }
            catch (JsonException)
            {
                // fall back to the old bare-command-name format, e.g. "IncrementPoints"
                message = Enum.TryParse<ScoreboardCommand>(text, ignoreCase: true, out var bareCommand)
                    ? new ScoreboardCommandMessage(bareCommand)
                    : null;
            }

            if (message is not null)
            {
                hub.Publish(message.Command, message.Payload);
            }
        }
    }
    catch (OperationCanceledException)
    {
        // control device dropped the connection without a clean close handshake (tab closed, network loss, etc.)
    }
    catch (WebSocketException)
    {
        // ditto - abrupt disconnect at the socket level
    }
    finally
    {
        hub.ControllerDisconnected();
    }
});

app.Run();
