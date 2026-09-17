namespace Zeymera.Scoreboard.Api.Responses;

public record MatchDto(
    int Id,
    int HomeTeamId,
    string HomeTeamName,
    int AwayTeamId,
    string AwayTeamName,
    int? HomeScore,
    int? AwayScore,
    DateTimeOffset PlayedAt);
