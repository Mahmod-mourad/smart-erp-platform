using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Core.Entities;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Infrastructure.Services;

public class TenantArchiveService(AppDbContext dbContext, IStorageService storageService) : ITenantArchiveService
{

    public async Task<string> CreateArchiveAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Fetch essential data
        var customers = await dbContext.Customers.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ToListAsync(cancellationToken);
        var branches = await dbContext.Branches.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ToListAsync(cancellationToken);
        var employees = await dbContext.Employees.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ToListAsync(cancellationToken);
        var products = await dbContext.Products.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ToListAsync(cancellationToken);
        var accounts = await dbContext.Accounts.IgnoreQueryFilters().Where(x => x.TenantId == tenantId).ToListAsync(cancellationToken);

        var archiveData = new
        {
            TenantId = tenantId,
            ExportDate = DateTime.UtcNow,
            Customers = customers,
            Branches = branches,
            Employees = employees,
            Products = products,
            Accounts = accounts
        };

        var fileName = $"tenant_{tenantId:N}_{DateTime.UtcNow:yyyyMMddHHmmss}.json";
        var json = JsonSerializer.Serialize(archiveData, new JsonSerializerOptions { WriteIndented = true });
        
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        var fileUrl = await storageService.UploadFileAsync("tenant-archives", fileName, stream, "application/json", cancellationToken);
        
        return fileUrl;
    }

    public async Task RestoreArchiveAsync(Guid tenantId, Stream archiveStream, CancellationToken cancellationToken = default)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var archiveData = await JsonSerializer.DeserializeAsync<TenantArchiveDto>(archiveStream, options, cancellationToken);
        
        if (archiveData == null || archiveData.TenantId != tenantId)
            throw new ApplicationException("Invalid backup file or mismatching Tenant ID.");

        using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // For safety, a real system would carefully upsert data to avoid conflicts.
            // In this MVP we insert records if they do not exist.
            
            // Customers
            if (archiveData.Customers != null)
            {
                var existingCustomers = await dbContext.Customers.IgnoreQueryFilters().Where(c => c.TenantId == tenantId).Select(c => c.Id).ToListAsync(cancellationToken);
                var newCustomers = archiveData.Customers.Where(c => !existingCustomers.Contains(c.Id)).ToList();
                if (newCustomers.Any()) dbContext.Customers.AddRange(newCustomers);
            }

            // Products
            if (archiveData.Products != null)
            {
                var existingProducts = await dbContext.Products.IgnoreQueryFilters().Where(p => p.TenantId == tenantId).Select(p => p.Id).ToListAsync(cancellationToken);
                var newProducts = archiveData.Products.Where(p => !existingProducts.Contains(p.Id)).ToList();
                if (newProducts.Any()) dbContext.Products.AddRange(newProducts);
            }

            // Accounts
            if (archiveData.Accounts != null)
            {
                var existingAccounts = await dbContext.Accounts.IgnoreQueryFilters().Where(a => a.TenantId == tenantId).Select(a => a.Id).ToListAsync(cancellationToken);
                var newAccounts = archiveData.Accounts.Where(a => !existingAccounts.Contains(a.Id)).ToList();
                if (newAccounts.Any()) dbContext.Accounts.AddRange(newAccounts);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private class TenantArchiveDto
    {
        public Guid TenantId { get; set; }
        public List<Customer>? Customers { get; set; }
        public List<Product>? Products { get; set; }
        public List<Account>? Accounts { get; set; }
    }
}
