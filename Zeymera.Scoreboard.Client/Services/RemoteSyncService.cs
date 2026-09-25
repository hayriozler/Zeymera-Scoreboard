using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Zeymera.Scoreboard.Client.Services;

public class RemoteSyncService(
    IDbContextFactory<DataContext> dbFactory,
    IHttpClientFactory httpClientFactory,
    IWebHostEnvironment env,
    IOptions<RemoteSyncOptions> options,
    ILogger<RemoteSyncService> logger) : BackgroundService
{
    private readonly RemoteSyncOptions _options = options.Value;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (!_options.Enabled)
            {
                logger.LogInformation("RemoteSync is disabled (RemoteSync:Enabled=false) - skipping remote data push.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            {
                logger.LogWarning("RemoteSync is enabled but RemoteSync:BaseUrl is empty - skipping remote data push.");
                return;
            }

            var baseUrl = _options.BaseUrl.EndsWith('/') ? _options.BaseUrl : _options.BaseUrl + "/";
            var http = httpClientFactory.CreateClient(nameof(RemoteSyncService));
            http.BaseAddress = new Uri(baseUrl);
            http.DefaultRequestHeaders.Add("X-Client-Id", _options.ClientId);

            var interval = TimeSpan.FromSeconds(Math.Max(5, _options.PollSeconds));
            using var timer = new PeriodicTimer(interval);
            do
            {
                try
                {
                    await SyncOnceAsync(http, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Remote sync tick failed; will retry next interval.");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RemoteSync could not start; remote data push is disabled for this run.");
        }
    }

    private async Task SyncOnceAsync(HttpClient http, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        await PushPlayersAsync(db, http, ct);
        await PushTeamsAsync(db, http, ct);
        await PushStatsAsync(db, http, ct);
        await CleanupSyncedMatchesAsync(db, ct);
    }

    private async Task PushTeamsAsync(DataContext db, HttpClient http, CancellationToken ct)
    {
        var pending = await db.Teams.Where(t => !t.SyncedAPI).ToListAsync(ct);
        foreach (var team in pending)
        {
            var payload = new
            {
                id = team.Id,
                name = team.Name
            };

            logger.LogInformation("SEND team {TeamId} to teams: {Name}", team.Id, team.Name);
            var response = await http.PostAsJsonAsync("teams", payload, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogInformation("RECEIVED response for team {TeamId}: {Status} {Body}", team.Id, response.StatusCode, body);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Push team {TeamId} failed: {Status}", team.Id, response.StatusCode);
                continue;
            }

            team.SyncedAPI = true;
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task PushPlayersAsync(DataContext db, HttpClient http, CancellationToken ct)
    {
        var pending = await db.Players.Where(p => !p.SyncedAPI).ToListAsync(ct);
        foreach (var player in pending)
        {
            string? photoBase64 = null;
            string? photoExtension = null;
            if (!string.IsNullOrEmpty(player.PhotoPath))
            {
                var fullPath = Path.Combine(env.WebRootPath, player.PhotoPath);
                if (File.Exists(fullPath))
                {
                    photoBase64 = Convert.ToBase64String(await File.ReadAllBytesAsync(fullPath, ct));
                    photoExtension = Path.GetExtension(fullPath).TrimStart('.');
                }
            }

            var payload = new
            {
                id = player.Id,
                nickname = player.Nickname,
                name = player.Name,
                avatarId = player.AvatarId,
                photoBase64,
                photoExtension
            };

            logger.LogInformation("SEND player {PlayerId} to players: nickname={Nickname} name={Name} avatarId={AvatarId} photoBytes={PhotoBytes}",
                player.Id, player.Nickname, player.Name, player.AvatarId, photoBase64?.Length ?? 0);
            var response = await http.PostAsJsonAsync("players", payload, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogInformation("RECEIVED response for player {PlayerId}: {Status} {Body}", player.Id, response.StatusCode, body);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Push player {PlayerId} failed: {Status}", player.Id, response.StatusCode);
                continue;
            }

            player.SyncedAPI = true;
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task PushStatsAsync(DataContext db, HttpClient http, CancellationToken ct)
    {
        var pending = await db.MatchResults.Where(m => !m.SyncedAPI).ToListAsync(ct);
        foreach (var match in pending)
        {
            var payload = new
            {
                player1Id = match.Player1Id,
                player1Name = match.Player1Name,
                player1Score = match.Player1Score,
                player1Avg = match.Player1Avg,
                player1HighRun = match.Player1HighRun,
                player2Id = match.Player2Id,
                player2Name = match.Player2Name,
                player2Score = match.Player2Score,
                player2Avg = match.Player2Avg,
                player2HighRun = match.Player2HighRun,
                inning = match.Inning,
                matchTarget = match.MatchTarget,
                winner = match.Winner,
                playedAt = match.PlayedAt
            };

            logger.LogInformation("SEND stats for match {MatchId} to stats: {Player1Name}({Player1Score}) vs {Player2Name}({Player2Score}) winner={Winner}",
                match.Id, match.Player1Name, match.Player1Score, match.Player2Name, match.Player2Score, match.Winner);
            var response = await http.PostAsJsonAsync("stats", payload, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogInformation("RECEIVED response for match {MatchId}: {Status} {Body}", match.Id, response.StatusCode, body);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Push stats for match {MatchId} failed: {Status}", match.Id, response.StatusCode);
                continue;
            }

            match.SyncedAPI = true;
            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task CleanupSyncedMatchesAsync(DataContext db, CancellationToken ct)
    {
        var finished = await db.MatchResults.Where(m => m.SyncedWS).ToListAsync(ct);
        if (finished.Count == 0)
        {
            return;
        }

        var finishedIds = finished.Select(m => m.Id).ToList();
        var finishedStats = await db.MatchScoreStats.Where(s => finishedIds.Contains(s.MatchResultId)).ToListAsync(ct);
        db.MatchScoreStats.RemoveRange(finishedStats);
        db.MatchResults.RemoveRange(finished);
        await db.SaveChangesAsync(ct);
    }
}
