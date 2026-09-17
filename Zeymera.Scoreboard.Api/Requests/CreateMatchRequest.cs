namespace Zeymera.Scoreboard.Api.Requests;

public record CreateMatchRequest(
    int HomeTeamId, int AwayTeamId,
    DateTimeOffset? PlayedAt);
