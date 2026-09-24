using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using System.Net.WebSockets;
using System.Text;
using Zeymera.Scoreboard.Client.Components;
using Zeymera.Scoreboard.Client.Models;
using Zeymera.Scoreboard.Client.Services;

var builder = WebApplication.CreateBuilder(args);

var logsFolder = Path.Combine(Directory.GetCurrentDirectory(), "logs");
Directory.CreateDirectory(logsFolder);

var logLevelSection = builder.Configuration.GetSection("Logging:LogLevel");
var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.Is(ParseLogLevel(logLevelSection["Default"]))
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(logsFolder, "scoreboard-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}{NewLine}    {Message:lj}{NewLine}{Exception}");

foreach (var category in logLevelSection.GetChildren())
{
    if (category.Key != "Default")
    {
        loggerConfig = loggerConfig.MinimumLevel.Override(category.Key, ParseLogLevel(category.Value));
    }
}

Log.Logger = loggerConfig.CreateLogger();
builder.Host.UseSerilog();

static LogEventLevel ParseLogLevel(string? value) => value?.ToLowerInvariant() switch
{
    "trace" => LogEventLevel.Verbose,
    "debug" => LogEventLevel.Debug,
    "warning" => LogEventLevel.Warning,
    "error" => LogEventLevel.Error,
    "critical" => LogEventLevel.Fatal,
    "none" => LogEventLevel.Fatal + 1,
    _ => LogEventLevel.Information,
};

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
builder.Services.Configure<RemoteSyncOptions>(builder.Configuration.GetSection("RemoteSync"));
builder.Services.AddHttpClient(nameof(RemoteSyncService));
builder.Services.AddHostedService<RemoteSyncService>();

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
            PhotoPath TEXT NULL,
            AvatarId INTEGER NULL,
            Synced INTEGER NOT NULL DEFAULT 0
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

app.UseWebSockets();
app.Map("/ws", async (HttpContext context, ScoreboardCommandHub hub, ILogger<Program> logger) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    logger.LogInformation("WS connected: {RemoteIp}", remoteIp);
    hub.ControllerConnected(socket, remoteIp);
    try
    {
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
                    logger.LogWarning("Message from {RemoteIp} exceeded {MaxBytes} bytes - closing connection", remoteIp, maxControlMessageBytes);
                    await socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, "message too large", CancellationToken.None);
                    closed = true;
                    break;
                }
            } while (!result.EndOfMessage);

            if (closed)
            {
                logger.LogInformation("WS closed by {RemoteIp}", remoteIp);
                break;
            }

            var seq = hub.NextSequence();
            var text = Encoding.UTF8.GetString(messageStream.ToArray());
            logger.LogInformation("[#{Seq}] RECV from {RemoteIp}: {Text}", seq, remoteIp, text);

            var messages = ScoreboardCommandParser.TryParse(text);
            if (messages.Count > 0)
            {
                logger.LogInformation("[#{Seq}] Parsed {Count} command(s) from {RemoteIp}: {Commands}", seq, messages.Count, remoteIp, string.Join(", ", messages.Select(m => m.Command)));
                hub.Publish(messages);
            }
            else
            {
                logger.LogWarning("[#{Seq}] Could not parse any command from {RemoteIp}: {Text}", seq, remoteIp, text);
            }
        }
    }
    catch (OperationCanceledException)
    {
        // control device dropped the connection without a clean close handshake (tab closed, network loss, etc.)
        logger.LogInformation("WS connection from {RemoteIp} cancelled (dropped without a close handshake)", remoteIp);
    }
    catch (WebSocketException ex)
    {
        // ditto - abrupt disconnect at the socket level
        logger.LogWarning(ex, "WS connection from {RemoteIp} ended abruptly", remoteIp);
    }
    finally
    {
        hub.ControllerDisconnected(socket);
        logger.LogInformation("WS disconnected: {RemoteIp}", remoteIp);
    }
});

app.Run();
