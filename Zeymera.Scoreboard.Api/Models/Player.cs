namespace Zeymera.Scoreboard.Api.Models;

public class Player
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int? TeamId { get; set; }

    public Team? Team { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
