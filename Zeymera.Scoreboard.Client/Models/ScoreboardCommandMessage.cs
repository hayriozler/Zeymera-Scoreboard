using System.Text.Json;

namespace Zeymera.Scoreboard.Client.Models;

public record ScoreboardCommandMessage(ScoreboardCommand Command, JsonElement? Payload = null);

public record PlayerUpsertPayload(int Id, string? Nickname = null, string? Name = null, string? PhotoBase64 = null, string? PhotoExtension = null);
