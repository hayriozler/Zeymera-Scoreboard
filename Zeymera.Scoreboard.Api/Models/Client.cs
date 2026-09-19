namespace Zeymera.Scoreboard.Api.Models;

public class Client
{
    public string Id { get; set; } = string.Empty;

    public string? Name { get; set; }

    public int? CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public int? TableNumber { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastSeenAt { get; set; }
}
