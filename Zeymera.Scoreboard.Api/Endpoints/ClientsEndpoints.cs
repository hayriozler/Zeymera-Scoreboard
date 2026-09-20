using Microsoft.EntityFrameworkCore;
using Zeymera.Scoreboard.Api.Data;
using Zeymera.Scoreboard.Api.Models;
using Zeymera.Scoreboard.Api.Requests;
using Zeymera.Scoreboard.Api.Responses;

namespace Zeymera.Scoreboard.Api.Endpoints;

public static class ClientsEndpoints
{
    public static RouteGroupBuilder MapClientsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clients").WithTags("Clients");

        group.MapGet("/", async (ScoreboardDbContext db, int? clubId) =>
        {
            var query = db.ClientSet.AsQueryable();
            if (clubId is not null)
            {
                query = query.Where(c => c.ClubId == clubId);
            }

            return await query
                .OrderBy(c => c.CreatedAt)
                .Select(c => new ClientDto(c.Id, c.Name, c.ClubId, c.TableNumber, c.CreatedAt, c.LastSeenAt))
                .ToListAsync();
        });

        group.MapPost("/", async (RegisterClientRequest request, ScoreboardDbContext db) =>
        {
            if (request.ClubId is not null && await db.ClubSet.FindAsync(request.ClubId) is null)
            {
                return Results.BadRequest("Club does not exist.");
            }

            string code;
            do
            {
                code = ClientCodeGenerator.Generate();
            } while (await db.ClientSet.AnyAsync(c => c.Id == code));

            var client = new Client
            {
                Id = code,
                Name = request.Name?.Trim(),
                ClubId = request.ClubId,
                TableNumber = request.TableNumber
            };
            db.ClientSet.Add(client);
            await db.SaveChangesAsync();

            return Results.Created($"/api/clients/{client.Id}", new ClientDto(client.Id, client.Name, client.ClubId, client.TableNumber, client.CreatedAt, client.LastSeenAt));
        });

        group.MapDelete("/{id}", async (string id, ScoreboardDbContext db) =>
        {
            var client = await db.ClientSet.FindAsync(id);
            if (client is null)
            {
                return Results.NotFound();
            }

            db.ClientSet.Remove(client);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return group;
    }
}
