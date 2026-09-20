namespace Zeymera.Scoreboard.Api.Models;

public class Club
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<Client> Clients { get; set; } = [];
}
