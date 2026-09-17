using System.Text.Json;
using Zeymera.Scoreboard.Client.Models;

namespace Zeymera.Scoreboard.Client.Services;

public class ScoreboardCommandHub
{
    private int _connectedControllers;

    public event Action<ScoreboardCommand, JsonElement?>? CommandReceived;
    public event Action? ConnectionChanged;

    public bool HasActiveConnection => Volatile.Read(ref _connectedControllers) > 0;

    public void Publish(ScoreboardCommand command, JsonElement? payload = null) => CommandReceived?.Invoke(command, payload);

    public void ControllerConnected()
    {
        Interlocked.Increment(ref _connectedControllers);
        ConnectionChanged?.Invoke();
    }

    public void ControllerDisconnected()
    {
        Interlocked.Decrement(ref _connectedControllers);
        ConnectionChanged?.Invoke();
    }
}
