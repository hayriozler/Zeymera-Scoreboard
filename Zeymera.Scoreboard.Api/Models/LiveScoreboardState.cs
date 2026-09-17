namespace Zeymera.Scoreboard.Api.Models;

public class LiveScoreboardState
{
    public string ClientId { get; set; } = string.Empty;

    public string Player1Name { get; set; } = string.Empty;

    public string Player2Name { get; set; } = string.Empty;

    public int Player1Score { get; set; }

    public int Player2Score { get; set; }

    public int Inning { get; set; }

    public int MatchTarget { get; set; }

    public double Player1Avg { get; set; }

    public int Player1HighRun { get; set; }

    public int CurrentPoints { get; set; }

    public double Player2BestAvg { get; set; }

    public int Player2HighRun { get; set; }

    public int ShotClockSeconds { get; set; }

    public double ShotClockRemaining { get; set; }

    public bool ShotClockActive { get; set; }

    public int ActivePlayer { get; set; } = 1;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
