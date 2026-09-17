using Zeymera.Scoreboard.Client.Models;

namespace Zeymera.Scoreboard.Client.Services;

/// <summary>
/// Singleton mediator: bridges commands from out-of-circuit sources (the WebSocket
/// control endpoint) into whichever Home.razor circuit(s) are currently rendering the board.
/// </summary>
public class ScoreboardCommandHub
{
    private int _connectedControllers;

    public event Action<ScoreboardCommand>? CommandReceived;
    public event Action? ConnectionChanged;

    public bool HasActiveConnection => Volatile.Read(ref _connectedControllers) > 0;

    public void Publish(ScoreboardCommand command) => CommandReceived?.Invoke(command);

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
