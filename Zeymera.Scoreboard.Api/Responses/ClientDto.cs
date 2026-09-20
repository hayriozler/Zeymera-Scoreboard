namespace Zeymera.Scoreboard.Api.Responses;

public record ClientDto(string Id, string? Name, int? ClubId, int? TableNumber, DateTimeOffset CreatedAt, DateTimeOffset? LastSeenAt);
