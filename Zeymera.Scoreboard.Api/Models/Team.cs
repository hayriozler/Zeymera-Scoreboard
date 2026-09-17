namespace Zeymera.Scoreboard.Api.Models;

public class Team
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<Match> HomeMatches { get; set; } = [];

    public List<Match> AwayMatches { get; set; } = [];

    public List<Player> Players { get; set; } = [];
}
