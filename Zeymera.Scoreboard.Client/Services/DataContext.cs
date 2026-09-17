using Microsoft.EntityFrameworkCore;
using Zeymera.Scoreboard.Client.Models;

namespace Zeymera.Scoreboard.Client.Services;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<ScoreboardState> ScoreboardStates { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<ScoreboardState>().HasKey(p => p.Id);
        modelBuilder.Entity<ScoreboardState>().Property(e => e.Id).UseAutoincrement();
        modelBuilder.Entity<ScoreboardState>().ToTable("scoreboard_state");
    }
}
