using Microsoft.EntityFrameworkCore;
using Zeymera.Scoreboard.Api.Data;
using Zeymera.Scoreboard.Api.Middlewares;
using Zeymera.Scoreboard.Api.Models;
using Zeymera.Scoreboard.Api.Requests;
using Zeymera.Scoreboard.Api.Responses;

namespace Zeymera.Scoreboard.Api.Endpoints;

public static class MatchStatsEndpoints
{
    public static RouteGroupBuilder MapMatchStatsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/matches/{matchId:int}/stat").WithTags("MatchStats");

        group.MapGet("/", async (int matchId, ScoreboardDbContext db) =>
            await db.MatchStatSet
                .Include(s => s.WinnerPlayer)
                .FirstOrDefaultAsync(s => s.MatchId == matchId) is { } stat
                ? Results.Ok(ToDto(stat))
                : Results.NotFound());

        group.MapPut("/", async (int matchId, RecordMatchStatRequest request, ScoreboardDbContext db, HttpContext context) =>
        {
            var match = await db.MatchSet.FindAsync(matchId);
            if (match is null)
            {
                return Results.NotFound();
            }

            if (request.WinnerPlayerCode is not null && await db.PlayerSet.FindAsync(request.WinnerPlayerCode) is null)
            {
                return Results.BadRequest("Winner player does not exist.");
            }

            var stat = await db.MatchStatSet.FirstOrDefaultAsync(s => s.MatchId == matchId);
            if (stat is null)
            {
                stat = new MatchStat { MatchId = matchId };
                db.MatchStatSet.Add(stat);
            }

            stat.HomeScore = request.HomeScore;
            stat.AwayScore = request.AwayScore;
            stat.WinnerPlayerCode = request.WinnerPlayerCode;
            stat.Duration = request.Duration;
            stat.ClientId = context.GetClientId();
            stat.RecordedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync();
            await db.Entry(stat).Reference(s => s.WinnerPlayer).LoadAsync();

            return Results.Ok(ToDto(stat));
        });

        return group;
    }

    private static MatchStatDto ToDto(MatchStat stat) => new(
        stat.Id,
        stat.MatchId,
        stat.HomeScore,
        stat.AwayScore,
        stat.WinnerPlayerCode,
        stat.WinnerPlayer?.Name,
        stat.Duration,
        stat.ClientId,
        stat.RecordedAt);
}
