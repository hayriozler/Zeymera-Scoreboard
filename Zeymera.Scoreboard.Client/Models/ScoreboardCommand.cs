namespace Zeymera.Scoreboard.Client.Models;

public enum ScoreboardCommand
{
    GetState,
    ToggleControls,
    ToggleShotClock,
    ResetShotClock,
    SelectPlayer1,
    SelectPlayer2,
    IncrementPoints,
    DecrementPoints,
    AdjustPoints,
    CommitPoints,
    RenamePlayer1,
    RenamePlayer2,
    UpsertPlayer,
    EndGame,
    NewGame,
    SetMatchTarget,
    SelectRosterPlayer1,
    SelectRosterPlayer2,
    ClearRosterPlayer1,
    ClearRosterPlayer2,
    WarmUp,
}
