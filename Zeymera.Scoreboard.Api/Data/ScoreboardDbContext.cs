using Microsoft.EntityFrameworkCore;
using Zeymera.Scoreboard.Api.Models;

namespace Zeymera.Scoreboard.Api.Data;

public class ScoreboardDbContext(DbContextOptions<ScoreboardDbContext> options) : DbContext(options)
{
    public DbSet<Team> TeamSet => Set<Team>();
    public DbSet<Match> MatchSet => Set<Match>();
    public DbSet<Player> PlayerSet => Set<Player>();
    public DbSet<MatchStat> MatchStatSet => Set<MatchStat>();
    public DbSet<Client> ClientSet => Set<Client>();
    public DbSet<LiveScoreboardState> LiveScoreboardStateSet => Set<LiveScoreboardState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Team>(entity =>
        {
            entity.Property(t => t.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(t => t.Name).IsUnique();
        });

        modelBuilder.Entity<Match>(entity =>
        {
            entity.HasOne(m => m.HomeTeam)
                .WithMany(t => t.HomeMatches)
                .HasForeignKey(m => m.HomeTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.AwayTeam)
                .WithMany(t => t.AwayMatches)
                .HasForeignKey(m => m.AwayTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.HomePlayer)
                .WithMany()
                .HasForeignKey(m => m.HomePlayerCode)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(m => m.AwayPlayer)
                .WithMany()
                .HasForeignKey(m => m.AwayPlayerCode)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(m => m.ClientId).HasMaxLength(10);

            entity.HasOne<Client>()
                .WithMany()
                .HasForeignKey(m => m.ClientId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(p => p.Code);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Code).IsRequired().HasMaxLength(25);

            entity.HasOne(p => p.Team)
                .WithMany(t => t.Players)
                .HasForeignKey(p => p.TeamId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MatchStat>(entity =>
        {
            entity.HasIndex(s => s.MatchId).IsUnique();

            entity.HasOne(s => s.Match)
                .WithOne(m => m.Stat)
                .HasForeignKey<MatchStat>(s => s.MatchId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.WinnerPlayer)
                .WithMany()
                .HasForeignKey(s => s.WinnerPlayerCode)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(s => s.ClientId).HasMaxLength(10);

            entity.HasOne<Client>()
                .WithMany()
                .HasForeignKey(s => s.ClientId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.Property(c => c.Id).HasMaxLength(10);
            entity.Property(c => c.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<LiveScoreboardState>(entity =>
        {
            entity.HasKey(s => s.ClientId);
            entity.Property(s => s.ClientId).HasMaxLength(10);
            entity.Property(s => s.Player1Name).HasMaxLength(100);
            entity.Property(s => s.Player2Name).HasMaxLength(100);

            entity.HasOne<Client>()
                .WithMany()
                .HasForeignKey(s => s.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
