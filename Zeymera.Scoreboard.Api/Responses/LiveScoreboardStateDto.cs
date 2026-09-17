namespace Zeymera.Scoreboard.Api.Responses;

public record LiveScoreboardStateDto(
    string ClientId,
    string Player1Name,
    string Player2Name,
    int Player1Score,
    int Player2Score,
    int Inning,
    int MatchTarget,
    double Player1Avg,
    int Player1HighRun,
    int CurrentPoints,
    double Player2BestAvg,
    int Player2HighRun,
    int ShotClockSeconds,
    double ShotClockRemaining,
    bool ShotClockActive,
    int ActivePlayer,
    DateTimeOffset UpdatedAt);
