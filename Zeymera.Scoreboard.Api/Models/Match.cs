namespace Zeymera.Scoreboard.Api.Models;

public class Match
{
    public int Id { get; set; }
    public string? HomePlayerCode { get; set; }
    public string? AwayPlayerCode { get; set; }
    public int HomeTeamId { get; set; }
    public Team? HomeTeam { get; set; }
    public Player? HomePlayer { get; set; }
    public Player? AwayPlayer { get; set; }
    public int AwayTeamId { get; set; }
    public Team? AwayTeam { get; set; }
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public int? TargetScore { get; set; }
    public DateTimeOffset PlayedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? ClientId { get; set; }
    public MatchStat? Stat { get; set; }
}


