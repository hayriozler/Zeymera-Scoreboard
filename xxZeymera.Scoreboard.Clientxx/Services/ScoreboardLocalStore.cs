using Microsoft.JSInterop;
using System.Text.Json;
using Zeymera.Scoreboard.Client.Models;

namespace Zeymera.Scoreboard.Client.Services;

public class ScoreboardLocalStore(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? _module;

    private async Task<IJSObjectReference> GetModuleAsync() =>
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/scoreboardDb.js");

    public async Task InitAsync()
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("init");
    }

    public async Task SaveAsync(ScoreboardState state)
    {
        var module = await GetModuleAsync();
        var json = JsonSerializer.Serialize(new
        {
            player1Name = state.Player1Name,
            player2Name = state.Player2Name,
            player1Score = state.Player1Score,
            player2Score = state.Player2Score,
            inning = state.Inning,
            matchTarget = state.MatchTarget,
            player1Avg = state.Player1Avg,
            player1HighRun = state.Player1HighRun,
            currentPoints = state.CurrentPoints,
            player2BestAvg = state.Player2Avg,
            player2HighRun = state.Player2HighRun,
            shotClockSeconds = state.ShotClockSeconds,
            shotClockRemaining = state.ShotClockRemaining,
            shotClockActive = state.ShotClockActive,
            updatedAt = state.UpdatedAt.ToString("O"),
        });
        await module.InvokeVoidAsync("saveState", json);
    }

    public async Task<ScoreboardState?> LoadAsync()
    {
        var module = await GetModuleAsync();
        var json = await module.InvokeAsync<string?>("loadState");
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new ScoreboardState
        {
            Player1Name = GetString(root, "player1_name") ?? "",
            Player2Name = GetString(root, "player2_name") ?? "",
            Player1Score = GetInt(root, "player1_score"),
            Player2Score = GetInt(root, "player2_score"),
            Inning = GetInt(root, "inning", 1),
            MatchTarget = GetInt(root, "match_target", 40),
            Player1Avg = GetDouble(root, "player1_avg"),
            Player1HighRun = GetInt(root, "player1_high_run"),
            CurrentPoints = GetInt(root, "current_points"),
            Player2Avg = GetDouble(root, "player2_avg"),
            Player2HighRun = GetInt(root, "player2_high_run"),
            ShotClockSeconds = GetInt(root, "shot_clock_seconds", 40),
            ShotClockRemaining = GetDouble(root, "shot_clock_remaining", 40),
            ShotClockActive = GetInt(root, "shot_clock_active") != 0,
            UpdatedAt = DateTimeOffset.TryParse(GetString(root, "updated_at"), out var updatedAt) ? updatedAt : DateTimeOffset.UtcNow,
        };
    }

    private static string? GetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static int GetInt(JsonElement root, string name, int fallback = 0) =>
        root.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.Number ? value.GetInt32() : fallback;

    private static double GetDouble(JsonElement root, string name, double fallback = 0) =>
        root.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.Number ? value.GetDouble() : fallback;

    public async Task MarkSyncedAsync()
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("markSynced");
    }

    public async Task<string?> GetClientIdAsync()
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<string?>("getClientId");
    }

    public async Task SetClientIdAsync(string id)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("setClientId", id);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.DisposeAsync();
        }
    }
}
