using Microsoft.EntityFrameworkCore;
using Zeymera.Scoreboard.Api.Data;
using Zeymera.Scoreboard.Api.Middlewares;
using Zeymera.Scoreboard.Api.Models;
using Zeymera.Scoreboard.Api.Requests;
using Zeymera.Scoreboard.Api.Responses;

namespace Zeymera.Scoreboard.Api.Endpoints;

public static class PlayersEndpoints
{
    public static RouteGroupBuilder MapPlayersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/players").WithTags("Players");

        group.MapGet("/", async (ScoreboardDbContext db, HttpContext context) =>
        {
            var clientId = context.GetClientId()!;
            return await db.PlayerSet
                .Where(p => p.ClientId == clientId)
                .OrderBy(p => p.ExternalId)
                .Select(p => ToDto(p))
                .ToListAsync();
        });

        group.MapPost("/", async (UpsertPlayerRequest request, ScoreboardDbContext db, HttpContext context, IWebHostEnvironment env) =>
        {
            var clientId = context.GetClientId()!;

            var player = await db.PlayerSet.FirstOrDefaultAsync(p => p.ClientId == clientId && p.ExternalId == request.Id);
            if (player is null)
            {
                player = new Player { ClientId = clientId, ExternalId = request.Id };
                db.PlayerSet.Add(player);
            }

            player.Nickname = request.Nickname;
            player.Name = request.Name;
            player.AvatarId = request.AvatarId;
            player.UpdatedAt = DateTimeOffset.UtcNow;

            if (!string.IsNullOrEmpty(request.PhotoBase64))
            {
                byte[] bytes;
                try
                {
                    bytes = Convert.FromBase64String(request.PhotoBase64);
                }
                catch (FormatException)
                {
                    return Results.BadRequest("Invalid PhotoBase64.");
                }

                var extension = string.IsNullOrWhiteSpace(request.PhotoExtension) ? "jpg" : request.PhotoExtension.TrimStart('.');
                var folder = Path.Combine(env.WebRootPath, "Players", clientId);
                Directory.CreateDirectory(folder);
                var fileName = $"{request.Id}.{extension}";
                await File.WriteAllBytesAsync(Path.Combine(folder, fileName), bytes);
                player.PhotoPath = $"Players/{clientId}/{fileName}";
            }

            await db.SaveChangesAsync();

            return Results.Ok(ToDto(player));
        });

        group.MapDelete("/{externalId:int}", async (int externalId, ScoreboardDbContext db, HttpContext context) =>
        {
            var clientId = context.GetClientId()!;
            var player = await db.PlayerSet.FirstOrDefaultAsync(p => p.ClientId == clientId && p.ExternalId == externalId);
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

    private static PlayerDto ToDto(Player p) =>
        new(p.Id, p.ClientId, p.ExternalId, p.Nickname, p.Name, p.PhotoPath, p.AvatarId, p.UpdatedAt);
}
