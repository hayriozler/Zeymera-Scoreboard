using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using Zeymera.Scoreboard.Client.Models;

namespace Zeymera.Scoreboard.Client.Services;

public class RemotePullService(
    IDbContextFactory<DataContext> dbFactory,
    IHttpClientFactory httpClientFactory,
    IOptions<RemoteSyncOptions> options,
    ILogger<RemotePullService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly RemoteSyncOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (!_options.Enabled)
            {
                logger.LogInformation("RemotePull is disabled (RemoteSync:Enabled=false) - skipping remote data pull.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            {
                logger.LogWarning("RemotePull is enabled but RemoteSync:BaseUrl is empty - skipping remote data pull.");
                return;
            }

            var baseUrl = _options.BaseUrl.EndsWith('/') ? _options.BaseUrl : _options.BaseUrl + "/";
            var http = httpClientFactory.CreateClient(nameof(RemotePullService));
            http.BaseAddress = new Uri(baseUrl);
            http.DefaultRequestHeaders.Add("X-Client-Id", _options.ClientId);

            var interval = TimeSpan.FromSeconds(Math.Max(5, _options.PollSeconds));
            using var timer = new PeriodicTimer(interval);
            do
            {
                try
                {
                    await PullOnceAsync(http, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Remote pull tick failed; will retry next interval.");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RemotePull could not start; remote data pull is disabled for this run.");
        }
    }

    private async Task PullOnceAsync(HttpClient http, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        await PullPlayersAsync(db, http, ct);
        await PullTeamsAsync(db, http, ct);
    }
    private static async Task PullTeamsAsync(DataContext db, HttpClient http, CancellationToken ct)
    {
        var remoteTeams = await http.GetFromJsonAsync<List<RemoteTeam>>("teams", _jsonOptions, ct);
        if (remoteTeams is null)
        {
            return;
        }

        foreach (var remote in remoteTeams)
        {
            var team = await db.Teams.FirstOrDefaultAsync(t => t.RemoteId == remote.Id, ct);
            if (team is null)
            {
                team = new Team { RemoteId = remote.Id };
                db.Teams.Add(team);
            }

            team.Name = remote.Name;
            await db.SaveChangesAsync(ct);

            var memberRemoteIds = remote.Players.Select(p => p.Id).ToHashSet();
            var members = await db.Players.Where(p => p.RemoteId != null && memberRemoteIds.Contains(p.RemoteId!.Value)).ToListAsync(ct);
            foreach (var member in members)
            {
                member.TeamId = team.Id;
            }

            var formerMembers = await db.Players.Where(p => p.TeamId == team.Id && !memberRemoteIds.Contains(p.RemoteId ?? -1)).ToListAsync(ct);
            foreach (var former in formerMembers)
            {
                former.TeamId = null;
            }

            await db.SaveChangesAsync(ct);
        }
    }

    private async Task PullPlayersAsync(DataContext db, HttpClient http, CancellationToken ct)
    {
        var remotePlayers = await http.GetFromJsonAsync<List<RemotePlayer>>("players", _jsonOptions, ct);
        if (remotePlayers is null)
        {
            return;
        }

        foreach (var remote in remotePlayers)
        {
            await UpsertPulledPlayerAsync(db, remote, ct);
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task<Player> UpsertPulledPlayerAsync(DataContext db, RemotePlayer remote, CancellationToken ct)
    {
        var player = await db.Players.FirstOrDefaultAsync(p => p.RemoteId == remote.Id, ct);

        if (player is null && remote.ClientId == _options.ClientId)
        {
            player = await db.Players.FirstOrDefaultAsync(p => p.Id == remote.ExternalId && p.RemoteId == null, ct);
        }

        if (player is null)
        {
            player = new Player { Synced = true };
            db.Players.Add(player);
        }

        player.RemoteId = remote.Id;

        if (player.Synced)
        {
            player.Nickname = remote.Nickname;
            player.Name = remote.Name;
            player.AvatarId = remote.AvatarId;
        }

        return player;
    }
    private record RemotePlayer(int Id, string ClientId, int ExternalId, string Nickname, string Name, int? AvatarId);
    private record RemoteTeam(int Id, string ClientId, int ExternalId, string Name, List<RemoteTeamPlayer> Players);
    private record RemoteTeamPlayer(int Id, int PlayerId, string Nickname, string Name);
}
