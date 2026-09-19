using Microsoft.EntityFrameworkCore;
using Zeymera.Scoreboard.Api.Data;
using Zeymera.Scoreboard.Api.Models;
using Zeymera.Scoreboard.Api.Requests;
using Zeymera.Scoreboard.Api.Responses;

namespace Zeymera.Scoreboard.Api.Endpoints;

public static class CustomersEndpoints
{
    public static RouteGroupBuilder MapCustomersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers").WithTags("Customers");

        group.MapGet("/", async (ScoreboardDbContext db) =>
            await db.CustomerSet
                .OrderBy(c => c.Name)
                .Select(c => new CustomerDto(c.Id, c.Name, c.CreatedAt))
                .ToListAsync());

        group.MapGet("/{id:int}", async (int id, ScoreboardDbContext db) =>
            await db.CustomerSet.FindAsync(id) is { } customer
                ? Results.Ok(new CustomerDto(customer.Id, customer.Name, customer.CreatedAt))
                : Results.NotFound());

        group.MapPost("/", async (CreateCustomerRequest request, ScoreboardDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest("Customer name is required.");
            }

            var customer = new Customer { Name = request.Name.Trim() };
            db.CustomerSet.Add(customer);
            await db.SaveChangesAsync();

            return Results.Created($"/api/customers/{customer.Id}", new CustomerDto(customer.Id, customer.Name, customer.CreatedAt));
        });

        group.MapDelete("/{id:int}", async (int id, ScoreboardDbContext db) =>
        {
            var customer = await db.CustomerSet.FindAsync(id);
            if (customer is null)
            {
                return Results.NotFound();
            }

            db.CustomerSet.Remove(customer);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return group;
    }
}
