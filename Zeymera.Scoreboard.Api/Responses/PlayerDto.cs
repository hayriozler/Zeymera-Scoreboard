namespace Zeymera.Scoreboard.Api.Responses;

public record PlayerDto(int Id, string ClientId, int ExternalId, string Nickname, string Name, string? PhotoPath, int? AvatarId, DateTimeOffset UpdatedAt);
