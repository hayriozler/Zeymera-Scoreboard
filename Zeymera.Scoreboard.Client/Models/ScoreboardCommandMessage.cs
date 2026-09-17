namespace Zeymera.Scoreboard.Client.Models;

/// <summary>Wire envelope for /ws/control: {"command":"RenamePlayer1","payload":"John"}. Payload is null/omitted for commands that don't need one.</summary>
public record ScoreboardCommandMessage(ScoreboardCommand Command, string? Payload = null);
