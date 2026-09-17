using Zeymera.Scoreboard.Client.Components;
using Zeymera.Scoreboard.Client.Infrastructure;
using Zeymera.Scoreboard.Client.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7153/";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddScoped<ScoreboardLocalStore>();
builder.Services.AddScoped<ScoreboardSyncService>();
builder.Services.AddScoped<LocalizationService>();
builder.Services.AddSingleton<WebSocketService>();

var app = builder.Build();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
