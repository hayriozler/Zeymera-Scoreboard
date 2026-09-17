using System.Text.Json;

namespace Zeymera.Scoreboard.Client.Models;

/// <summary>
/// Wire envelope for /ws/control and the Bluetooth remote: {"command":"RenamePlayer1","payload":"John"}.
/// Payload is a raw JsonElement so different commands can carry different shapes - a bare string for
/// the rename commands, or a structured object (see PlayerUpsertPayload) for UpsertPlayer. Omitted/null
/// for commands that don't need one.
/// </summary>
public record ScoreboardCommandMessage(ScoreboardCommand Command, JsonElement? Payload = null);

/// <summary>Payload shape for ScoreboardCommand.UpsertPlayer. PhotoBase64 is optional - omit it to update
/// just the name/nickname of an existing player without touching their photo.</summary>
public record PlayerUpsertPayload(int Id, string? Nickname = null, string? Name = null, string? PhotoBase64 = null, string? PhotoExtension = null);
