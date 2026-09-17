namespace Zeymera.Scoreboard.Api.Responses;

public record ClientDto(string Id, string? Name, DateTimeOffset CreatedAt, DateTimeOffset? LastSeenAt);
