namespace Zeymera.Scoreboard.Api.Models;

public class Player
{
    public int Id { get; set; }

    public string ClientId { get; set; } = string.Empty;

    public int ExternalId { get; set; }

    public string Nickname { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? PhotoPath { get; set; }

    public int? AvatarId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
