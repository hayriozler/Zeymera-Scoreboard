namespace Zeymera.Scoreboard.Api.Requests;

public record RecordMatchStatRequest(int HomeScore, int AwayScore, string? WinnerPlayerCode, TimeSpan? Duration);