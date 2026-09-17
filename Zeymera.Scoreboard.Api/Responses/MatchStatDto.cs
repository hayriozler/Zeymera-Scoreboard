namespace Zeymera.Scoreboard.Api.Responses;

public record MatchStatDto(
    int Id,
    int MatchId,
    int HomeScore,
    int AwayScore,
    string? WinnerPlayerCode,
    string? WinnerPlayerName,
    TimeSpan? Duration,
    string? ClientId,
    DateTimeOffset RecordedAt);
