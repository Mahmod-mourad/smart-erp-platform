using NexaFlow.API.Infrastructure;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Constants;

namespace NexaFlow.API.Endpoints;

public static class ProductsEndpoints
{
    public static IEndpointRouteBuilder MapProductsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products")
            .RequireAuthorization();

        group.MapGet("/", async (IInventoryService svc) =>
            Results.Ok(await svc.GetAllAsync()))
            .WithSummary("List products for the current tenant.");

        group.MapGet("/low-stock", async (IInventoryService svc) =>
            Results.Ok(await svc.GetLowStockAsync()))
            .RequireAuthorization(AppPolicies.RequireManager)
            .WithSummary("List products at or below their minimum stock (Manager+).");

        group.MapGet("/{id:guid}", async (Guid id, IInventoryService svc) =>
            Results.Ok(await svc.GetByIdAsync(id)))
            .WithSummary("Get a single product by id.");

        group.MapPost("/", async (CreateProductDto req, IInventoryService svc) =>
            Results.Ok(await svc.CreateAsync(req)))
            .AddEndpointFilter<ValidationFilter<CreateProductDto>>()
            .RequireAuthorization(AppPolicies.RequireManager)
            .WithSummary("Create a product (Manager+).");

        group.MapPut("/{id:guid}", async (Guid id, UpdateProductDto req, IInventoryService svc) =>
            Results.Ok(await svc.UpdateAsync(id, req)))
            .AddEndpointFilter<ValidationFilter<UpdateProductDto>>()
            .RequireAuthorization(AppPolicies.RequireManager)
            .WithSummary("Update a product (Manager+).");

        group.MapPost("/{id:guid}/movements", async (Guid id, AddStockMovementDto req, IInventoryService svc) =>
            Results.Ok(await svc.AddMovementAsync(id, req)))
            .AddEndpointFilter<ValidationFilter<AddStockMovementDto>>()
            .WithSummary("Record a stock In/Out movement; returns the updated product.");

        group.MapGet("/{id:guid}/movements", async (Guid id, IInventoryService svc) =>
            Results.Ok(await svc.GetMovementsAsync(id)))
            .WithSummary("List a product's stock movements.");

        group.MapDelete("/{id:guid}", async (Guid id, IInventoryService svc) =>
        {
            await svc.DeleteAsync(id);
            return Results.NoContent();
        })
            .RequireAuthorization(AppPolicies.RequireCompanyAdmin)
            .WithSummary("Delete a product (Company Admin).");

        return app;
    }
}
