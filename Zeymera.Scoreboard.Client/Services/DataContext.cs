using Microsoft.EntityFrameworkCore;
using Zeymera.Scoreboard.Client.Models;

namespace Zeymera.Scoreboard.Client.Services;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<ScoreboardState> ScoreboardStates { get; set; }
    public DbSet<Player> Players { get; set; }
    public DbSet<MatchResult> MatchResults { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<ScoreboardState>().HasKey(p => p.Id);
        modelBuilder.Entity<ScoreboardState>().Property(e => e.Id).UseAutoincrement();
        modelBuilder.Entity<ScoreboardState>().ToTable("scoreboard_state");

        modelBuilder.Entity<Player>().HasKey(p => p.Id);
        modelBuilder.Entity<Player>().Property(p => p.Id).UseAutoincrement();
        modelBuilder.Entity<Player>().ToTable("player");

        modelBuilder.Entity<MatchResult>().HasKey(m => m.Id);
        modelBuilder.Entity<MatchResult>().Property(m => m.Id).UseAutoincrement();
        modelBuilder.Entity<MatchResult>().ToTable("match_result");
    }
}
