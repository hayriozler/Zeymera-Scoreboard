using Microsoft.EntityFrameworkCore;
using Zeymera.Scoreboard.Api.Data;
using Zeymera.Scoreboard.Api.Middlewares;
using Zeymera.Scoreboard.Api.Models;
using Zeymera.Scoreboard.Api.Requests;
using Zeymera.Scoreboard.Api.Responses;

namespace Zeymera.Scoreboard.Api.Endpoints;

public static class TeamsEndpoints
{
    public static RouteGroupBuilder MapTeamsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/teams").WithTags("Teams");

        group.MapGet("/", async (ScoreboardDbContext db, HttpContext context) =>
        {
            var clientId = context.GetClientId()!;
            var clubId = context.GetClubId();

            var query = clubId is null
                ? db.TeamSet.Where(t => t.ClientId == clientId)
                : db.TeamSet.Where(t => db.ClientSet.Any(c => c.Id == t.ClientId && c.ClubId == clubId));

            var teams = await query
                .OrderBy(t => t.ExternalId)
                .Include(t => t.TeamPlayers)
                .ThenInclude(tp => tp.Player)
                .ToListAsync();

            return teams.Select(ToDto).ToList();
        });

        group.MapPost("/", async (UpsertTeamRequest request, ScoreboardDbContext db, HttpContext context) =>
        {
            var clientId = context.GetClientId()!;

            var team = await db.TeamSet.FirstOrDefaultAsync(t => t.ClientId == clientId && t.ExternalId == request.Id);
            if (team is null)
            {
                team = new Team { ClientId = clientId, ExternalId = request.Id };
                db.TeamSet.Add(team);
            }

            team.Name = request.Name;
            team.UpdatedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(ToDto(await LoadWithPlayersAsync(db, team.Id)));
        });

        group.MapDelete("/{externalId:int}", async (int externalId, ScoreboardDbContext db, HttpContext context) =>
        {
            var clientId = context.GetClientId()!;
            var team = await db.TeamSet.FirstOrDefaultAsync(t => t.ClientId == clientId && t.ExternalId == externalId);
            if (team is null)
            {
                return Results.NotFound();
            }

            db.TeamSet.Remove(team);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapPut("/{externalId:int}/players", async (int externalId, SetTeamPlayersRequest request, ScoreboardDbContext db, HttpContext context) =>
        {
            var clientId = context.GetClientId()!;
            var team = await db.TeamSet
                .Include(t => t.TeamPlayers)
                .FirstOrDefaultAsync(t => t.ClientId == clientId && t.ExternalId == externalId);
            if (team is null)
            {
                return Results.NotFound();
            }

            var requestedPlayerIds = request.PlayerIds.Distinct().ToArray();
            var players = await db.PlayerSet
                .Where(p => p.ClientId == clientId && requestedPlayerIds.Contains(p.ExternalId))
                .ToListAsync();

            if (players.Count != requestedPlayerIds.Length)
            {
                return Results.BadRequest("One or more players were not found for this client.");
            }

            db.TeamPlayerSet.RemoveRange(team.TeamPlayers);
            foreach (var player in players)
            {
                db.TeamPlayerSet.Add(new TeamPlayer { TeamId = team.Id, PlayerId = player.Id });
            }

            await db.SaveChangesAsync();

            return Results.Ok(ToDto(await LoadWithPlayersAsync(db, team.Id)));
        });

        group.MapDelete("/{externalId:int}/players/{playerExternalId:int}", async (int externalId, int playerExternalId, ScoreboardDbContext db, HttpContext context) =>
        {
            var clientId = context.GetClientId()!;
            var team = await db.TeamSet.FirstOrDefaultAsync(t => t.ClientId == clientId && t.ExternalId == externalId);
            if (team is null)
            {
                return Results.NotFound();
            }

            var player = await db.PlayerSet.FirstOrDefaultAsync(p => p.ClientId == clientId && p.ExternalId == playerExternalId);
            if (player is null)
            {
                return Results.NotFound();
            }

            var teamPlayer = await db.TeamPlayerSet.FirstOrDefaultAsync(tp => tp.TeamId == team.Id && tp.PlayerId == player.Id);
            if (teamPlayer is null)
            {
                return Results.NotFound();
            }

            db.TeamPlayerSet.Remove(teamPlayer);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return group;
    }

    private static Task<Team> LoadWithPlayersAsync(ScoreboardDbContext db, int teamId) =>
        db.TeamSet
            .Include(t => t.TeamPlayers)
            .ThenInclude(tp => tp.Player)
            .FirstAsync(t => t.Id == teamId);

    private static TeamDto ToDto(Team team) => new(
        team.Id, team.ClientId, team.ExternalId, team.Name, team.UpdatedAt,
        team.TeamPlayers.Select(tp => new TeamPlayerDto(tp.Player!.Id, tp.Player.ExternalId, tp.Player.Nickname, tp.Player.Name)).ToList());
}
