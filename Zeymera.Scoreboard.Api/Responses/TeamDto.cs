namespace Zeymera.Scoreboard.Api.Responses;

public record TeamDto(int Id, string ClientId, int ExternalId, string Name, DateTimeOffset UpdatedAt, List<TeamPlayerDto> Players);

public record TeamPlayerDto(int Id, int PlayerId, string Nickname, string Name);
