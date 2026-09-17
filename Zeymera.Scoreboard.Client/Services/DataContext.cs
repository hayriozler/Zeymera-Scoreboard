using Microsoft.EntityFrameworkCore;
using Zeymera.Scoreboard.Client.Models;

namespace Zeymera.Scoreboard.Client.Services;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<ScoreboardState> ScoreboardStates { get; set; }
    public DbSet<Player> Players { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<ScoreboardState>().HasKey(p => p.Id);
        modelBuilder.Entity<ScoreboardState>().Property(e => e.Id).UseAutoincrement();
        modelBuilder.Entity<ScoreboardState>().ToTable("scoreboard_state");

        modelBuilder.Entity<Player>().HasKey(p => p.Id);
        modelBuilder.Entity<Player>().Property(p => p.Id).UseAutoincrement();
        modelBuilder.Entity<Player>().ToTable("player");
    }
}
