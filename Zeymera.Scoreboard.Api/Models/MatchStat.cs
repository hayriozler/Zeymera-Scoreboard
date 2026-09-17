namespace Zeymera.Scoreboard.Api.Models;

public class MatchStat
{
    public int Id { get; set; }

    public int MatchId { get; set; }

    public Match? Match { get; set; }

    public int HomeScore { get; set; }

    public int AwayScore { get; set; }

    public string? WinnerPlayerCode { get; set; }

    public Player? WinnerPlayer { get; set; }

    public TimeSpan? Duration { get; set; }

    public string? ClientId { get; set; }

    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
}
