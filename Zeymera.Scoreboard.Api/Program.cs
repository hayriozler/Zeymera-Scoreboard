using Microsoft.EntityFrameworkCore;
using Zeymera.Scoreboard.Api.Data;
using Zeymera.Scoreboard.Api.Endpoints;
using Zeymera.Scoreboard.Api.Middlewares;

var builder = WebApplication.CreateBuilder(args);

const string clientCorsPolicy = "Client";

builder.Services.AddOpenApi();

builder.Services.AddDbContext<ScoreboardDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddCors(options =>
    options.AddPolicy(clientCorsPolicy, policy => policy
        .WithOrigins(builder.Configuration.GetSection("ClientOrigins").Get<string[]>() ??
            ["https://localhost:7272", "http://localhost:5098"])
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

app.Environment.WebRootPath = Directory.GetCurrentDirectory();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors(clientCorsPolicy);

app.UseStaticFiles();

app.UseMiddleware<ClientIdMiddleware>();

app.MapClubsEndpoints();
app.MapClientsEndpoints();
app.MapPlayersEndpoints();
app.MapMatchStatsEndpoints();
app.MapTeamsEndpoints();

app.Run();
