using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zeymera.Scoreboard.Client.Models;

/// <summary>Parses incoming control text (from the WebSocket endpoint or the Bluetooth remote) into a
/// ScoreboardCommandMessage. Shared so both input paths agree on exactly one wire format.</summary>
public static class ScoreboardCommandParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static ScoreboardCommandMessage? TryParse(string text)
    {
        text = text.Trim();
        if (text.Length == 0)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ScoreboardCommandMessage>(text, Options);
        }
        catch (JsonException)
        {
            // fall back to the old bare-command-name format, e.g. "IncrementPoints"
            return Enum.TryParse<ScoreboardCommand>(text, ignoreCase: true, out var bareCommand)
                ? new ScoreboardCommandMessage(bareCommand)
                : null;
        }
    }
}
