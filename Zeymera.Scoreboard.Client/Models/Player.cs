namespace Zeymera.Scoreboard.Client.Models;

public class Player
{
    public int Id { get; set; }
    public string Nickname { get; set; } = "";
    public string Name { get; set; } = "";
    public string? PhotoPath { get; set; }
    public int? AvatarId { get; set; }
    public bool Synced { get; set; }
}
