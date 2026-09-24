namespace Zeymera.Scoreboard.Client.Models;

/// <summary>A finished match, snapshotted from ScoreboardState when "Finish Match" is pressed.
/// Player names are copied as text (not just Player1Id/Player2Id) so history stays readable
/// even if the roster player is later renamed or deleted.</summary>
public class MatchResult
{
    public int Id { get; set; }

    /// <summary>DateTime, not DateTimeOffset - EF Core's SQLite provider can't translate ORDER BY on a DateTimeOffset column.</summary>
    public DateTime PlayedAt { get; set; } = DateTime.Now;

    public int? Player1Id { get; set; }
    public string Player1Name { get; set; } = "";
    public int Player1Score { get; set; }
    public double Player1Avg { get; set; }
    public int Player1HighRun { get; set; }

    public int? Player2Id { get; set; }
    public string Player2Name { get; set; } = "";
    public int Player2Score { get; set; }
    public double Player2Avg { get; set; }
    public int Player2HighRun { get; set; }

    public int Inning { get; set; }
    public int MatchTarget { get; set; }

    /// <summary>1 or 2, or 0 for a tie.</summary>
    public int Winner { get; set; }

    public bool SyncedAPI { get; set; }
    public bool SyncedWS { get; set; }
}
