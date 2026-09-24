using Microsoft.EntityFrameworkCore;
using Zeymera.Scoreboard.Api.Models;

namespace Zeymera.Scoreboard.Api.Data;

public class ScoreboardDbContext(DbContextOptions<ScoreboardDbContext> options) : DbContext(options)
{
    public DbSet<Club> ClubSet => Set<Club>();
    public DbSet<Client> ClientSet => Set<Client>();
    public DbSet<Player> PlayerSet => Set<Player>();
    public DbSet<MatchStat> MatchStatSet => Set<MatchStat>();
    public DbSet<Team> TeamSet => Set<Team>();
    public DbSet<TeamPlayer> TeamPlayerSet => Set<TeamPlayer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Club>(entity =>
        {
            entity.Property(c => c.ClientId).IsRequired().HasMaxLength(10);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(c => c.ClientId).IsUnique();
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.Property(c => c.Id).HasMaxLength(10);
            entity.Property(c => c.Name).HasMaxLength(200);

            entity.HasOne(c => c.Club)
                .WithMany(cl => cl.Clients)
                .HasForeignKey(c => c.ClubId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Player>(entity =>
        {
            entity.Property(p => p.ClientId).HasMaxLength(10);
            entity.Property(p => p.Nickname).HasMaxLength(100);
            entity.Property(p => p.Name).HasMaxLength(200);
            entity.HasIndex(p => new { p.ClientId, p.ExternalId }).IsUnique();

            entity.HasOne<Client>()
                .WithMany()
                .HasForeignKey(p => p.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MatchStat>(entity =>
        {
            entity.Property(s => s.ClientId).HasMaxLength(10);
            entity.Property(s => s.Player1Name).HasMaxLength(100);
            entity.Property(s => s.Player2Name).HasMaxLength(100);

            entity.HasOne<Client>()
                .WithMany()
                .HasForeignKey(s => s.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.Property(t => t.ClientId).HasMaxLength(10);
            entity.Property(t => t.Name).HasMaxLength(200);
            entity.HasIndex(t => new { t.ClientId, t.ExternalId }).IsUnique();

            entity.HasOne<Client>()
                .WithMany()
                .HasForeignKey(t => t.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TeamPlayer>(entity =>
        {
            entity.HasKey(tp => new { tp.TeamId, tp.PlayerId });

            entity.HasOne(tp => tp.Team)
                .WithMany(t => t.TeamPlayers)
                .HasForeignKey(tp => tp.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(tp => tp.Player)
                .WithMany()
                .HasForeignKey(tp => tp.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
