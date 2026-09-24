using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using Zeymera.Scoreboard.Client.Models;

namespace Zeymera.Scoreboard.Client.Services;

public class ScoreboardCommandHub(ILogger<ScoreboardCommandHub> logger)
{
    private sealed record Connection(SemaphoreSlim Gate, string RemoteIp);

    private readonly ConcurrentDictionary<WebSocket, Connection> _sockets = new();
    private long _sequence;

    public event Action<WebSocket, IReadOnlyList<ScoreboardCommandMessage>>? CommandReceived;
    public event Action? ConnectionChanged;

    public bool HasActiveConnection => !_sockets.IsEmpty;

    public long NextSequence() => Interlocked.Increment(ref _sequence);

    public void Publish(WebSocket sender, IReadOnlyList<ScoreboardCommandMessage> messages)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Publishing {Count} command(s) to the board: {Commands}", messages.Count, string.Join(", ", messages.Select(m => m.Command)));
        }

        CommandReceived?.Invoke(sender, messages);
    }

    public void ControllerConnected(WebSocket socket, string remoteIp)
    {
        _sockets[socket] = new Connection(new SemaphoreSlim(1, 1), remoteIp);
        logger.LogInformation("Hub: registered client {RemoteIp} ({Count} total)", remoteIp, _sockets.Count);
        ConnectionChanged?.Invoke();
    }

    public void ControllerDisconnected(WebSocket socket)
    {
        if (_sockets.TryRemove(socket, out var connection))
        {
            connection.Gate.Dispose();
            logger.LogInformation("Hub: unregistered client {RemoteIp} ({Count} remaining)", connection.RemoteIp, _sockets.Count);
        }

        ConnectionChanged?.Invoke();
    }

    private static readonly TimeSpan _sendTimeout = TimeSpan.FromSeconds(5);

    public async Task SendAsync(WebSocket socket, string json, CancellationToken cancellationToken = default)
    {
        if (!_sockets.TryGetValue(socket, out var connection))
        {
            logger.LogWarning("Hub: SendAsync called for an unregistered socket - message dropped");
            return;
        }

        var seq = NextSequence();
        await connection.Gate.WaitAsync(cancellationToken);
        try
        {
            if (socket.State == WebSocketState.Open)
            {
                using var timeoutCts = new CancellationTokenSource(_sendTimeout);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                await socket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, linkedCts.Token);
                logger.LogInformation("[#{Seq}] SENT to {RemoteIp}: {Json}", seq, connection.RemoteIp, json);
            }
            else
            {
                logger.LogWarning("[#{Seq}] Skipped send to {RemoteIp} - socket state is {State}", seq, connection.RemoteIp, socket.State);
            }
        }
        catch (WebSocketException ex)
        {
            logger.LogWarning(ex, "[#{Seq}] Send to {RemoteIp} failed (WebSocketException)", seq, connection.RemoteIp);
        }
        catch (ObjectDisposedException)
        {
            logger.LogWarning("[#{Seq}] Send to {RemoteIp} failed - socket already disposed", seq, connection.RemoteIp);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("[#{Seq}] Send to {RemoteIp} timed out after {Timeout}", seq, connection.RemoteIp, _sendTimeout);
        }
        finally
        {
            connection.Gate.Release();
        }
    }

    public async Task BroadcastStateAsync(ScoreboardStateSnapshot snapshot, WebSocket? excludeSocket = null)
    {
        var targets = _sockets.Keys.Where(socket => socket != excludeSocket).ToList();
        if (targets.Count == 0)
        {
            return;
        }

        var json = ScoreboardStateSnapshotFactory.ToWireJson(snapshot);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Broadcasting state to {Count} client(s): {RemoteIps}", targets.Count, string.Join(", ", targets.Select(socket => _sockets[socket].RemoteIp)));
        }

        await Task.WhenAll(targets.Select(socket => SendAsync(socket, json)));
    }

    public async Task BroadcastJsonAsync(string json)
    {
        var targets = _sockets.Keys.ToList();
        if (targets.Count == 0)
        {
            return;
        }

        await Task.WhenAll(targets.Select(socket => SendAsync(socket, json)));
    }
}
