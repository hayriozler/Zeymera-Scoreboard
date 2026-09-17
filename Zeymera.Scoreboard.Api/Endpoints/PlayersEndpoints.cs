using Microsoft.EntityFrameworkCore;
using Zeymera.Scoreboard.Api.Data;
using Zeymera.Scoreboard.Api.Models;
using Zeymera.Scoreboard.Api.Requests;
using Zeymera.Scoreboard.Api.Responses;

namespace Zeymera.Scoreboard.Api.Endpoints;

public static class PlayersEndpoints
{
    public static RouteGroupBuilder MapPlayersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/players").WithTags("Players");

        group.MapGet("/", async (ScoreboardDbContext db, int? teamId) =>
        {
            var query = db.PlayerSet.Include(p => p.Team).AsQueryable();
            if (teamId is not null)
            {
                query = query.Where(p => p.TeamId == teamId);
            }

            return await query
                .OrderBy(p => p.Name)
                .Select(p => new PlayerDto(p.Code, p.Name, p.TeamId, p.Team!.Name, p.CreatedAt))
                .ToListAsync();
        });

        group.MapGet("/{code}", async (string code, ScoreboardDbContext db) =>
            await db.PlayerSet.Include(p => p.Team).FirstOrDefaultAsync(p => p.Code == code) is { } player
                ? Results.Ok(new PlayerDto(player.Code, player.Name, player.TeamId, player.Team!.Name, player.CreatedAt))
                : Results.NotFound());

        group.MapPost("/", async (CreatePlayerRequest request, ScoreboardDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest("Player name is required.");
            }

            var team = await db.TeamSet.FindAsync(request.TeamId);
            if (team is null)
            {
                return Results.BadRequest("Team does not exist.");
            }

            var player = new Player { Name = request.Name.Trim(), TeamId = request.TeamId };
            db.PlayerSet.Add(player);
            await db.SaveChangesAsync();

            return Results.Created($"/api/players/{player.Code}", new PlayerDto(player.Code, player.Name, player.TeamId, team.Name, player.CreatedAt));
        });

        group.MapDelete("/{code}", async (string code, ScoreboardDbContext db) =>
        {
            var player = await db.PlayerSet.FindAsync(code);
            if (player is null)
            {
                return Results.NotFound();
            }

            db.PlayerSet.Remove(player);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return group;
    }
}
