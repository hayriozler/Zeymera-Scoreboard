namespace Zeymera.Scoreboard.Client.Models;

public class Player
{
    /// <summary>Caller-assigned - not database-generated.</summary>
    public int Id { get; set; }
    public string Nickname { get; set; } = "";

    /// <summary>Relative path under wwwroot to the photo file on disk (e.g. "Players/7.jpg"), or null if none set.</summary>
    public string? PhotoPath { get; set; }
}
