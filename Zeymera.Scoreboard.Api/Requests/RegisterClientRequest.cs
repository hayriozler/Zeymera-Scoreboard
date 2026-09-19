namespace Zeymera.Scoreboard.Api.Requests;

public record RegisterClientRequest(string? Name, int? CustomerId, int? TableNumber);