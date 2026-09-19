using Microsoft.JSInterop;
using Zeymera.Scoreboard.Client.Models;

namespace Zeymera.Scoreboard.Client.Services;

public class BluetoothService(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? _module;
    private DotNetObjectReference<BluetoothService>? _selfRef;

    public event Action<IReadOnlyList<ScoreboardCommandMessage>>? CommandReceived;
    public event Action? ConnectionChanged;

    public bool IsConnected { get; private set; }
    public string? DeviceName { get; private set; }

    public async Task ConnectAsync()
    {
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/bluetooth.js");
        _selfRef ??= DotNetObjectReference.Create(this);
        try
        {
            await _module.InvokeVoidAsync("connect", _selfRef);
        }
        catch (JSException)
        {
        }
    }

    public async Task DisconnectAsync()
    {
        if (_module is not null)
        {
            await _module.InvokeVoidAsync("disconnect");
        }
    }

    [JSInvokable]
    public void OnCommand(string raw)
    {
        var messages = ScoreboardCommandParser.TryParse(raw);
        if (messages.Count > 0)
        {
            CommandReceived?.Invoke(messages);
        }
    }

    [JSInvokable]
    public void OnConnected(string deviceName)
    {
        IsConnected = true;
        DeviceName = deviceName;
        ConnectionChanged?.Invoke();
    }

    [JSInvokable]
    public void OnDisconnected()
    {
        IsConnected = false;
        ConnectionChanged?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module is not null)
            {
                await _module.InvokeVoidAsync("disconnect");
                await _module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
        }

        _selfRef?.Dispose();
    }
}
