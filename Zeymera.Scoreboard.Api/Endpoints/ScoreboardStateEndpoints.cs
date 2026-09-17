using Zeymera.Scoreboard.Api.Data;
using Zeymera.Scoreboard.Api.Middlewares;
using Zeymera.Scoreboard.Api.Models;
using Zeymera.Scoreboard.Api.Requests;
using Zeymera.Scoreboard.Api.Responses;

namespace Zeymera.Scoreboard.Api.Endpoints;

public static class ScoreboardStateEndpoints
{
    public static RouteGroupBuilder MapScoreboardStateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/scoreboard/state").WithTags("ScoreboardState");

        group.MapGet("/{clientId}", async (string clientId, ScoreboardDbContext db) =>
            await db.LiveScoreboardStateSet.FindAsync(clientId) is { } state
                ? Results.Ok(ToDto(state))
                : Results.NotFound());

        group.MapPut("/", async (UpsertLiveScoreboardStateRequest request, ScoreboardDbContext db, HttpContext context) =>
        {
            var clientId = context.GetClientId()!;

            var state = await db.LiveScoreboardStateSet.FindAsync(clientId);
            if (state is null)
            {
                state = new LiveScoreboardState { ClientId = clientId };
                db.LiveScoreboardStateSet.Add(state);
            }

            state.Player1Name = request.Player1Name;
            state.Player2Name = request.Player2Name;
            state.Player1Score = request.Player1Score;
            state.Player2Score = request.Player2Score;
            state.Inning = request.Inning;
            state.MatchTarget = request.MatchTarget;
            state.Player1Avg = request.Player1Avg;
            state.Player1HighRun = request.Player1HighRun;
            state.CurrentPoints = request.CurrentPoints;
            state.Player2BestAvg = request.Player2BestAvg;
            state.Player2HighRun = request.Player2HighRun;
            state.ShotClockSeconds = request.ShotClockSeconds;
            state.ShotClockRemaining = request.ShotClockRemaining;
            state.ShotClockActive = request.ShotClockActive;
            state.ActivePlayer = request.ActivePlayer;
            state.UpdatedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(ToDto(state));
        });

        return group;
    }

    private static LiveScoreboardStateDto ToDto(LiveScoreboardState s) => new(
        s.ClientId,
        s.Player1Name,
        s.Player2Name,
        s.Player1Score,
        s.Player2Score,
        s.Inning,
        s.MatchTarget,
        s.Player1Avg,
        s.Player1HighRun,
        s.CurrentPoints,
        s.Player2BestAvg,
        s.Player2HighRun,
        s.ShotClockSeconds,
        s.ShotClockRemaining,
        s.ShotClockActive,
        s.ActivePlayer,
        s.UpdatedAt);
}
