using Microsoft.EntityFrameworkCore;
using Zeymera.Scoreboard.Api.Api;
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors(clientCorsPolicy);

app.UseMiddleware<ClientIdMiddleware>();

app.MapClientsEndpoints();
app.MapTeamsEndpoints();
app.MapPlayersEndpoints();
app.MapMatchesEndpoints();
app.MapMatchStatsEndpoints();
app.MapScoreboardStateEndpoints();

app.Run();