namespace Zeymera.Scoreboard.Api.Requests;

public record UpsertPlayerRequest(int Id, string Nickname, string Name, int? AvatarId, string? PhotoBase64, string? PhotoExtension);
