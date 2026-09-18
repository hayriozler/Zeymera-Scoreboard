namespace Zeymera.Scoreboard.Client.Models;

public enum ScoreboardCommand
{
    ToggleControls,
    ToggleShotClock,
    ResetShotClock,
    SelectPlayer1,
    SelectPlayer2,
    IncrementPoints,
    DecrementPoints,
    CommitPoints,
    RenamePlayer1,
    RenamePlayer2,
    UpsertPlayer,
    EndGame,
    NewGame,
    SetMatchTarget,
}
