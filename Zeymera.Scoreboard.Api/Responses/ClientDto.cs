namespace Zeymera.Scoreboard.Api.Responses;

public record ClientDto(string Id, string? Name, int? CustomerId, int? TableNumber, DateTimeOffset CreatedAt, DateTimeOffset? LastSeenAt);
