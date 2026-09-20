using Microsoft.EntityFrameworkCore;
using Zeymera.Scoreboard.Api.Data;
using Zeymera.Scoreboard.Api.Models;
using Zeymera.Scoreboard.Api.Requests;
using Zeymera.Scoreboard.Api.Responses;

namespace Zeymera.Scoreboard.Api.Endpoints;

public static class ClubsEndpoints
{
    public static RouteGroupBuilder MapClubsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clubs").WithTags("Clubs");

        group.MapGet("/", async (ScoreboardDbContext db) =>
            await db.ClubSet
                .OrderBy(c => c.Name)
                .Select(c => new ClubDto(c.Id, c.Name, c.CreatedAt))
                .ToListAsync());

        group.MapGet("/{id:int}", async (int id, ScoreboardDbContext db) =>
            await db.ClubSet.FindAsync(id) is { } club
                ? Results.Ok(new ClubDto(club.Id, club.Name, club.CreatedAt))
                : Results.NotFound());

        group.MapPost("/", async (CreateClubRequest request, ScoreboardDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest("Club name is required.");
            }

            var club = new Club { Name = request.Name.Trim() };
            db.ClubSet.Add(club);
            await db.SaveChangesAsync();

            return Results.Created($"/api/clubs/{club.Id}", new ClubDto(club.Id, club.Name, club.CreatedAt));
        });

        group.MapDelete("/{id:int}", async (int id, ScoreboardDbContext db) =>
        {
            var club = await db.ClubSet.FindAsync(id);
            if (club is null)
            {
                return Results.NotFound();
            }

            db.ClubSet.Remove(club);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return group;
    }
}
