using System.Net;
using System.Net.Http.Json;
using Zeymera.Scoreboard.Client.Models;

namespace Zeymera.Scoreboard.Client.Services;

public class ScoreboardSyncService(HttpClient http, ScoreboardLocalStore localStore) : IAsyncDisposable
{
    private const string _clientIdHeader = "X-Client-Id";
    private static readonly TimeSpan _syncInterval = TimeSpan.FromSeconds(5);

    private static readonly TimeSpan _shotClockTickInterval = TimeSpan.FromMilliseconds(100);

    private Timer? _timer;
    private Timer? _shotClockTimer;
    private string? _clientId;
    private bool _syncing;

    public ScoreboardState Current { get; private set; } = new();

    public bool IsOnline { get; private set; }

    public event Action? StateChanged;

    public async Task StartAsync()
    {
        try
        {
            await localStore.InitAsync();

            var saved = await localStore.LoadAsync();
            if (saved is not null)
            {
                Current = saved;
            }

            _clientId = await localStore.GetClientIdAsync();
        }
        catch
        {
        }

        _timer = new Timer(OnTick, null, _syncInterval, _syncInterval);
    }

    public void Mutate(Action<ScoreboardState> mutate)
    {
        mutate(Current);
        Current.UpdatedAt = DateTimeOffset.UtcNow;
        StateChanged?.Invoke();
    }

    public void StartShotClock()
    {
        if (Current.ShotClockRemaining <= 0)
        {
            Current.ShotClockRemaining = Current.ShotClockSeconds;
        }

        Current.ShotClockActive = true;
        _shotClockTimer ??= new Timer(OnShotClockTick, null, _shotClockTickInterval, _shotClockTickInterval);
        StateChanged?.Invoke();
    }

    public void StopShotClock()
    {
        Current.ShotClockActive = false;
        StateChanged?.Invoke();
    }

    public void ResetShotClock()
    {
        Current.ShotClockRemaining = Current.ShotClockSeconds;
        Current.ShotClockActive = false;
        StateChanged?.Invoke();
    }

    private void OnShotClockTick(object? _)
    {
        if (!Current.ShotClockActive)
        {
            return;
        }

        Current.ShotClockRemaining = Math.Max(0, Current.ShotClockRemaining - _shotClockTickInterval.TotalSeconds);
        if (Current.ShotClockRemaining <= 0)
        {
            Current.ShotClockActive = false;
        }

        StateChanged?.Invoke();
    }

    private void OnTick(object? _) => _ = TickAsync();

    private async Task TickAsync()
    {
        if (_syncing)
        {
            return;
        }

        _syncing = true;
        try
        {
            await localStore.SaveAsync(Current);

            try
            {
                await EnsureClientRegisteredAsync();

                using var request = new HttpRequestMessage(HttpMethod.Put, "/api/scoreboard/state")
                {
                    Content = JsonContent.Create(new
                    {
                        player1Name = Current.Player1Name,
                        player2Name = Current.Player2Name,
                        player1Score = Current.Player1Score,
                        player2Score = Current.Player2Score,
                        inning = Current.Inning,
                        matchTarget = Current.MatchTarget,
                        player1Avg = Current.Player1Avg,
                        player1HighRun = Current.Player1HighRun,
                        currentPoints = Current.CurrentPoints,
                        player2Avg = Current.Player2Avg,
                        player2HighRun = Current.Player2HighRun,
                        shotClockSeconds = Current.ShotClockSeconds,
                        shotClockRemaining = Current.ShotClockRemaining,
                        shotClockActive = Current.ShotClockActive,
                        activePlayer = Current.ActivePlayer,
                    }),
                };
                request.Headers.Add(_clientIdHeader, _clientId);

                using var response = await http.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    await localStore.MarkSyncedAsync();
                    IsOnline = true;
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _clientId = null;
                    IsOnline = false;
                }
                else
                {
                    IsOnline = false;
                }
            }
            catch
            {
                IsOnline = false;
            }
        }
        finally
        {
            _syncing = false;
            StateChanged?.Invoke();
        }
    }

    private async Task EnsureClientRegisteredAsync()
    {
        if (!string.IsNullOrEmpty(_clientId))
        {
            return;
        }

        var response = await http.PostAsJsonAsync("/api/clients", new { Name = "Scoreboard Display" });
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<ClientRegistrationDto>();
        if (dto is not null)
        {
            _clientId = dto.Id;
            await localStore.SetClientIdAsync(dto.Id);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_timer is not null)
        {
            await _timer.DisposeAsync();
        }

        if (_shotClockTimer is not null)
        {
            await _shotClockTimer.DisposeAsync();
        }
    }

    private sealed record ClientRegistrationDto(string Id, string? Name, DateTimeOffset CreatedAt, DateTimeOffset? LastSeenAt);
}
