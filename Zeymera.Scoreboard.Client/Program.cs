using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Zeymera.Scoreboard.Client.Components;
using Zeymera.Scoreboard.Client.Endpoints;
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

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Db");
if (!Directory.Exists(folder))
    Directory.CreateDirectory(folder);
var dbName = Path.Combine(folder, "scoreboard.db3");

string playerPhotosFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Players");
if (!Directory.Exists(playerPhotosFolder))
    Directory.CreateDirectory(playerPhotosFolder);
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7153/";
builder.Services.AddDbContextFactory<DataContext>(options => options.UseSqlite($"Data Source={dbName}"));
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddScoped<LocalizationService>();
builder.Services.AddSingleton<WebSocketService>();
builder.Services.AddSingleton<ScoreboardCommandHub>();
builder.Services.AddSingleton<SystemPowerService>();
builder.Services.Configure<RemoteSyncOptions>(builder.Configuration.GetSection("RemoteSync"));
builder.Services.AddHttpClient(nameof(RemoteSyncService));
builder.Services.AddHostedService<RemoteSyncService>();
builder.Services.AddHttpClient(nameof(RemotePullService));
builder.Services.AddHostedService<RemotePullService>();
builder.Services.AddHostedService<RemoteWsPushService>();

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
            RemoteId INTEGER NULL,
            PhotoPath TEXT NULL,
            AvatarId INTEGER NULL,
            AvatarName TEXT NULL,
            TeamId INTEGER NULL,
            ShortcutNumber INTEGER NULL,
            SyncedAPI INTEGER NOT NULL DEFAULT 0,
            SyncedWS INTEGER NOT NULL DEFAULT 0
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
            Winner INTEGER NOT NULL,
            SyncedAPI INTEGER NOT NULL DEFAULT 0,
            SyncedWS INTEGER NOT NULL DEFAULT 0
        );
        """);

    db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS team (
            Id INTEGER PRIMARY KEY,
            Name TEXT NOT NULL DEFAULT '',
            RemoteId INTEGER NULL,
            SyncedAPI INTEGER NOT NULL DEFAULT 0,
            SyncedWS INTEGER NOT NULL DEFAULT 0
        );
        """);

    db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS score_event (
            Id INTEGER PRIMARY KEY,
            Timestamp TEXT NOT NULL,
            PlayerSlot INTEGER NOT NULL,
            Points INTEGER NOT NULL
        );
        """);

    db.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS match_score_stat (
            Id INTEGER PRIMARY KEY,
            MatchResultId INTEGER NOT NULL,
            PlayerSlot INTEGER NOT NULL,
            BucketIndex INTEGER NOT NULL,
            TotalPoints INTEGER NOT NULL
        );
        """);

}
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.UseWebSockets();
app.MapScoreboardWebSocket();

app.Run();
