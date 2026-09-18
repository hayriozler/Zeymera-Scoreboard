using Microsoft.EntityFrameworkCore;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
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
    // tables introduced after the first run - create them here explicitly.
    db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS scoreboard_state (
            Id INTEGER PRIMARY KEY,
            Player1Name TEXT NOT NULL DEFAULT '',
            Player2Name TEXT NOT NULL DEFAULT '',
            Player1Id INTEGER NULL,
            Player2Id INTEGER NULL,
            Player1Score INTEGER NOT NULL,
            Player2Score INTEGER NOT NULL,
            Inning INTEGER NOT NULL,
            MatchTarget INTEGER NOT NULL,
            Player1Avg REAL NOT NULL,
            Player1HighRun INTEGER NOT NULL,
            CurrentPoints INTEGER NOT NULL,
            Player2Avg REAL NOT NULL,
            Player2HighRun INTEGER NOT NULL,
            ShotClockSeconds INTEGER NOT NULL,
            ShotClockRemaining REAL NOT NULL,
            ShotClockActive INTEGER NOT NULL,
            ActivePlayer INTEGER NOT NULL
        );
        """);

    db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS player (
            Id INTEGER PRIMARY KEY,
            Nickname TEXT NOT NULL,
            Name TEXT NOT NULL DEFAULT '',
            PhotoPath TEXT NULL
        );
        """);


    db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS match_result (
            Id INTEGER PRIMARY KEY,
            PlayedAt TEXT NOT NULL,
            Player1Id INTEGER NULL,
            Player1Name TEXT NOT NULL DEFAULT '',
            Player1Score INTEGER NOT NULL,
            Player1Avg REAL NOT NULL,
            Player1HighRun INTEGER NOT NULL,
            Player2Id INTEGER NULL,
            Player2Name TEXT NOT NULL DEFAULT '',
            Player2Score INTEGER NOT NULL,
            Player2Avg REAL NOT NULL,
            Player2HighRun INTEGER NOT NULL,
            Inning INTEGER NOT NULL,
            MatchTarget INTEGER NOT NULL,
            Winner INTEGER NOT NULL
        );
        """);
}
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

const int maxControlMessageBytes = 16 * 1024 * 1024; // generous headroom for a base64-encoded player photo

var wsJsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

app.UseWebSockets();
app.Map("/ws", async (HttpContext context, ScoreboardCommandHub hub, IDbContextFactory<DataContext> dbFactory) =>
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
        await using (var db = await dbFactory.CreateDbContextAsync(context.RequestAborted))
        {
            var snapshot = await BuildSnapshotAsync(db);
            if (snapshot is not null)
            {
                var stateJson = JsonSerializer.Serialize(new { type = "state", state = snapshot }, wsJsonOptions);
                if (socket.State == WebSocketState.Open)
                    await socket.SendAsync(Encoding.UTF8.GetBytes(stateJson), WebSocketMessageType.Text, true, context.RequestAborted);
            }
        }

        var buffer = new byte[8192];
        while (socket.State == WebSocketState.Open)
        {
            using var messageStream = new MemoryStream();
            WebSocketReceiveResult result;
            var closed = false;
            do
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), context.RequestAborted);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                    closed = true;
                    break;
                }

                messageStream.Write(buffer, 0, result.Count);
                if (messageStream.Length > maxControlMessageBytes)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, "message too large", CancellationToken.None);
                    closed = true;
                    break;
                }
            } while (!result.EndOfMessage);

            if (closed)
            {
                break;
            }

            var text = Encoding.UTF8.GetString(messageStream.ToArray());
            var message = ScoreboardCommandParser.TryParse(text);
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

static async Task<ScoreboardStateSnapshot?> BuildSnapshotAsync(DataContext db)
{
    var state = await db.ScoreboardStates.FirstOrDefaultAsync(s => s.Id == 1);
    if (state is null)
    {
        return null;
    }

    var player1 = state.Player1Id is int id1 ? await db.Players.FirstOrDefaultAsync(p => p.Id == id1) : null;
    var player2 = state.Player2Id is int id2 ? await db.Players.FirstOrDefaultAsync(p => p.Id == id2) : null;

    static string ResolveName(Player? player, string manualName, string fallback)
    {
        if (player is not null)
        {
            return string.IsNullOrWhiteSpace(player.Nickname) ? player.Name : player.Nickname;
        }
        return string.IsNullOrWhiteSpace(manualName) ? fallback : manualName;
    }

    return new ScoreboardStateSnapshot(
        ResolveName(player1, state.Player1Name, "Player 1"),
        ResolveName(player2, state.Player2Name, "Player 2"),
        state.Player1Score,
        state.Player2Score,
        state.Player1Avg,
        state.Player2Avg,
        state.Player1HighRun,
        state.Player2HighRun,
        state.ActivePlayer,
        state.CurrentPoints,
        state.Inning,
        state.MatchTarget,
        state.ShotClockActive,
        state.ShotClockRemaining,
        state.ShotClockSeconds);
}