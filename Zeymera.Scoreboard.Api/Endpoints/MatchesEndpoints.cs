using Microsoft.EntityFrameworkCore;
using Zeymera.Scoreboard.Api.Data;
using Zeymera.Scoreboard.Api.Middlewares;
using Zeymera.Scoreboard.Api.Models;
using Zeymera.Scoreboard.Api.Requests;
using Zeymera.Scoreboard.Api.Responses;

namespace Zeymera.Scoreboard.Api.Endpoints;

public static class MatchesEndpoints
{
    public static RouteGroupBuilder MapMatchesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/matches").WithTags("Matches");

        group.MapGet("/", async (ScoreboardDbContext db) =>
            await db.MatchSet
                .OrderByDescending(m => m.PlayedAt)
                .Select(m => new MatchDto(
                    m.Id,
                    m.HomeTeamId,
                    m.HomeTeam!.Name,
                    m.AwayTeamId,
                    m.AwayTeam!.Name,
                    m.HomeScore,
                    m.AwayScore,
                    m.PlayedAt))
                .ToListAsync());

        group.MapGet("/{id:int}", async (int id, ScoreboardDbContext db) =>
            await db.MatchSet
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .FirstOrDefaultAsync(m => m.Id == id) is { } match
                ? Results.Ok(new MatchDto(
                    match.Id,
                    match.HomeTeamId,
                    match.HomeTeam!.Name,
                    match.AwayTeamId,
                    match.AwayTeam!.Name,
                    match.HomeScore,
                    match.AwayScore,
                    match.PlayedAt))
                : Results.NotFound());

        group.MapPost("/", async (CreateMatchRequest request, ScoreboardDbContext db, HttpContext context) =>
        {
            if (request.HomeTeamId == request.AwayTeamId)
            {
                return Results.BadRequest("A team cannot play against itself.");
            }

            var homeTeam = await db.TeamSet.FindAsync(request.HomeTeamId);
            var awayTeam = await db.TeamSet.FindAsync(request.AwayTeamId);
            if (homeTeam is null || awayTeam is null)
            {
                return Results.BadRequest("Both teams must exist.");
            }

            var match = new Match
            {
                HomeTeamId = request.HomeTeamId,
                AwayTeamId = request.AwayTeamId,
                PlayedAt = request.PlayedAt ?? DateTimeOffset.UtcNow,
                ClientId = context.GetClientId(),
            };

            db.MatchSet.Add(match);
            await db.SaveChangesAsync();

            return Results.Created($"/api/matches/{match.Id}", new MatchDto(
                match.Id,
                match.HomeTeamId,
                homeTeam.Name,
                match.AwayTeamId,
                awayTeam.Name,
                match.HomeScore,
                match.AwayScore,
                match.PlayedAt));
        });

        group.MapDelete("/{id:int}", async (int id, ScoreboardDbContext db) =>
        {
            var match = await db.MatchSet.FindAsync(id);
            if (match is null)
            {
                return Results.NotFound();
            }

            db.MatchSet.Remove(match);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return group;
    }
}
