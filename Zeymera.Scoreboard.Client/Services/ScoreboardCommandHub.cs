using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Zeymera.Scoreboard.Client.Models;

namespace Zeymera.Scoreboard.Client.Services;

public class ScoreboardCommandHub
{
    private readonly ConcurrentDictionary<WebSocket, byte> _sockets = new();

    public event Action<ScoreboardCommand, JsonElement?>? CommandReceived;
    public event Action? ConnectionChanged;

    public bool HasActiveConnection => !_sockets.IsEmpty;

    public void Publish(ScoreboardCommand command, JsonElement? payload = null) => CommandReceived?.Invoke(command, payload);

    public void ControllerConnected(WebSocket socket)
    {
        _sockets[socket] = 0;
        ConnectionChanged?.Invoke();
    }

    public void ControllerDisconnected(WebSocket socket)
    {
        _sockets.TryRemove(socket, out _);
        ConnectionChanged?.Invoke();
    }

    /// <summary>Pushes a state snapshot to every currently connected WS control client - called
    /// whenever the board's state changes, regardless of what triggered the change (keyboard,
    /// on-screen controls, another WS client, etc.), so no connected client is left showing a
    /// stale view.</summary>
    public async Task BroadcastStateAsync(ScoreboardStateSnapshot snapshot)
    {
        if (_sockets.IsEmpty)
        {
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(ScoreboardStateSnapshotFactory.ToWireJson(snapshot));
        foreach (var socket in _sockets.Keys)
        {
            if (socket.State != WebSocketState.Open)
            {
                continue;
            }

            try
            {
                await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (WebSocketException)
            {
                // a dying socket will be unregistered by its own receive loop's finally block
            }
            catch (ObjectDisposedException)
            {
                // ditto - closed/disposed between the State check above and the send
            }
        }
    }
}
