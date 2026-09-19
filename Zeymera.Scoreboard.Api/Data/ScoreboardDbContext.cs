using Microsoft.EntityFrameworkCore;
using Zeymera.Scoreboard.Api.Models;

namespace Zeymera.Scoreboard.Api.Data;

public class ScoreboardDbContext(DbContextOptions<ScoreboardDbContext> options) : DbContext(options)
{
    public DbSet<Customer> CustomerSet => Set<Customer>();
    public DbSet<Client> ClientSet => Set<Client>();
    public DbSet<Player> PlayerSet => Set<Player>();
    public DbSet<MatchStat> MatchStatSet => Set<MatchStat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.Property(c => c.Id).HasMaxLength(10);
            entity.Property(c => c.Name).HasMaxLength(200);

            entity.HasOne(c => c.Customer)
                .WithMany(cu => cu.Clients)
                .HasForeignKey(c => c.CustomerId)
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
    }
}
