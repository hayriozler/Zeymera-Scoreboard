namespace Zeymera.Scoreboard.Api.Responses;

public record PlayerDto(string Code, string Name, int? TeamId, string? TeamName, DateTimeOffset CreatedAt);
