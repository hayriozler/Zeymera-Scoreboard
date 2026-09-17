using Microsoft.EntityFrameworkCore;
using Zeymera.Scoreboard.Api.Data;
using Zeymera.Scoreboard.Api.Models;
using Zeymera.Scoreboard.Api.Requests;
using Zeymera.Scoreboard.Api.Responses;

namespace Zeymera.Scoreboard.Api.Api;

public static class TeamsEndpoints
{
    public static RouteGroupBuilder MapTeamsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/teams").WithTags("Teams");

        group.MapGet("/", async (ScoreboardDbContext db) =>
            await db.TeamSet
                .OrderBy(t => t.Name)
                .Select(t => new TeamDto(t.Id, t.Name, t.CreatedAt))
                .ToListAsync());

        group.MapGet("/{id:int}", async (int id, ScoreboardDbContext db) =>
            await db.TeamSet.FindAsync(id) is { } team
                ? Results.Ok(new TeamDto(team.Id, team.Name, team.CreatedAt))
                : Results.NotFound());

        group.MapPost("/", async (CreateTeamRequest request, ScoreboardDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest("Team name is required.");
            }

            var team = new Team { Name = request.Name.Trim() };
            db.TeamSet.Add(team);
            await db.SaveChangesAsync();

            return Results.Created($"/api/teams/{team.Id}", new TeamDto(team.Id, team.Name, team.CreatedAt));
        });

        group.MapDelete("/{id:int}", async (int id, ScoreboardDbContext db) =>
        {
            var team = await db.TeamSet.FindAsync(id);
            if (team is null)
            {
                return Results.NotFound();
            }

            db.TeamSet.Remove(team);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return group;
    }
}
