using Microsoft.EntityFrameworkCore;
using NexaFlow.Application.Common.Exceptions;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Entities;
using NexaFlow.Core.Enums;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Infrastructure.Services;

public class InventoryService(AppDbContext db, ICurrentUser currentUser) : IInventoryService
{
    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await db.Products.OrderBy(p => p.Name).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<ProductDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
                      ?? throw new NotFoundException("Product not found.");
        return ToDto(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto request, CancellationToken ct = default)
    {
        var tenantId = currentUser.TenantId
                       ?? throw new UnauthorizedAppException("No tenant in the current context.");

        var product = new Product
        {
            TenantId = tenantId,
            Name = request.Name,
            SKU = request.SKU,
            Category = request.Category,
            UnitPrice = request.UnitPrice,
            MinimumStock = request.MinimumStock,
            Description = request.Description,
            CurrentStock = 0,
            IsLowStock = request.MinimumStock > 0
        };

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);

        return ToDto(product);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto request, CancellationToken ct = default)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
                      ?? throw new NotFoundException("Product not found.");

        product.Name = request.Name;
        product.UnitPrice = request.UnitPrice;
        product.MinimumStock = request.MinimumStock;
        product.Description = request.Description;
        product.IsLowStock = product.CurrentStock < product.MinimumStock;

        await db.SaveChangesAsync(ct);
        return ToDto(product);
    }

    public async Task<ProductDto> AddMovementAsync(Guid productId, AddStockMovementDto request, CancellationToken ct = default)
    {
        var tenantId = currentUser.TenantId
                       ?? throw new UnauthorizedAppException("No tenant in the current context.");

        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId, ct)
                      ?? throw new NotFoundException("Product not found.");

        var type = Enum.Parse<StockMovementType>(request.Type);

        if (type == StockMovementType.In)
        {
            product.CurrentStock += request.Quantity;
        }
        else
        {
            if (request.Quantity > product.CurrentStock)
                throw new ConflictException("Cannot remove more stock than is currently available.");
            product.CurrentStock -= request.Quantity;
        }

        product.IsLowStock = product.CurrentStock < product.MinimumStock;

        db.StockMovements.Add(new StockMovement
        {
            TenantId = tenantId,
            ProductId = productId,
            Type = type,
            Quantity = request.Quantity,
            Reason = request.Reason,
            CreatedById = currentUser.UserId ?? Guid.Empty
        });

        await db.SaveChangesAsync(ct);
        return ToDto(product);
    }

    public async Task<IReadOnlyList<StockMovementDto>> GetMovementsAsync(Guid productId, CancellationToken ct = default)
    {
        var exists = await db.Products.AnyAsync(p => p.Id == productId, ct);
        if (!exists)
            throw new NotFoundException("Product not found.");

        var rows = await db.StockMovements
            .Where(m => m.ProductId == productId)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new
            {
                m.Id, m.Type, m.Quantity, m.Reason, m.CreatedAt,
                CreatedByName = db.Users.Where(u => u.Id == m.CreatedById)
                    .Select(u => u.FirstName + " " + u.LastName).FirstOrDefault()
            })
            .ToListAsync(ct);

        return rows.Select(m => new StockMovementDto(
            m.Id, m.Type.ToString(), m.Quantity, m.Reason, m.CreatedAt,
            string.IsNullOrWhiteSpace(m.CreatedByName) ? null : m.CreatedByName.Trim())).ToList();
    }

    public async Task<IReadOnlyList<ProductDto>> GetLowStockAsync(CancellationToken ct = default)
    {
        var rows = await db.Products.Where(p => p.IsLowStock).OrderBy(p => p.Name).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
                      ?? throw new NotFoundException("Product not found.");
        db.Products.Remove(product);
        await db.SaveChangesAsync(ct);
    }

    private static ProductDto ToDto(Product p) => new(
        p.Id, p.Name, p.SKU, p.Category, p.UnitPrice,
        p.CurrentStock, p.MinimumStock, p.IsLowStock, p.CreatedAt);
}
