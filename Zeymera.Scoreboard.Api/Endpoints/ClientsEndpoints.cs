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

        group.MapGet("/", async (ScoreboardDbContext db) =>
            await db.ClientSet
                .OrderBy(c => c.CreatedAt)
                .Select(c => new ClientDto(c.Id, c.Name, c.CreatedAt, c.LastSeenAt))
                .ToListAsync());

        group.MapPost("/", async (RegisterClientRequest request, ScoreboardDbContext db) =>
        {
            string code;
            do
            {
                code = ClientCodeGenerator.Generate();
            } while (await db.ClientSet.AnyAsync(c => c.Id == code));

            var client = new Client { Id = code, Name = request.Name?.Trim() };
            db.ClientSet.Add(client);
            await db.SaveChangesAsync();

            return Results.Created($"/api/clients/{client.Id}", new ClientDto(client.Id, client.Name, client.CreatedAt, client.LastSeenAt));
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
