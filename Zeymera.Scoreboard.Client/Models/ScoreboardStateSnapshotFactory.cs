using System.Text.Json;

namespace Zeymera.Scoreboard.Client.Models;

/// <summary>Single place that builds a ScoreboardStateSnapshot and its wire envelope - used both by
/// the WS endpoint's connect-time snapshot (built from the DB, no live circuit to read from) and
/// Home.razor's broadcast-on-change (built from live in-memory state), so the two can never drift
/// out of sync with each other.</summary>
public static class ScoreboardStateSnapshotFactory
{
    public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static ScoreboardStateSnapshot Create(ScoreboardState state, Player? player1, Player? player2)
    {
        return new ScoreboardStateSnapshot(
            ResolveName(player1, state.Player1Name, "Player 1"),
            ResolveName(player2, state.Player2Name, "Player 2"),
            state.Player1Score,
            state.Player2Score,
            state.Player1Avg,
            state.Player2Avg,
            state.Player1HighRun,
            state.Player2HighRun,
            state.ActivePlayer,
            state.CurrentPoints,
            state.Inning,
            state.MatchTarget,
            state.ShotClockActive,
            state.ShotClockSeconds);
    }

    public static string ToWireJson(ScoreboardStateSnapshot snapshot) =>
        JsonSerializer.Serialize(new { type = "state", state = snapshot }, JsonOptions);

    private static string ResolveName(Player? player, string manualName, string fallback)
    {
        if (player is not null)
        {
            return string.IsNullOrWhiteSpace(player.Nickname) ? player.Name : player.Nickname;
        }
        return string.IsNullOrWhiteSpace(manualName) ? fallback : manualName;
    }
}
