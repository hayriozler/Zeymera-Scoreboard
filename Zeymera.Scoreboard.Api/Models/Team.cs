namespace Zeymera.Scoreboard.Api.Models;

public class Team
{
    public int Id { get; set; }

    public string ClientId { get; set; } = string.Empty;

    public int ExternalId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<TeamPlayer> TeamPlayers { get; set; } = [];
}
