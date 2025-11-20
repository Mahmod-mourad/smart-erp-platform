using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Core.Entities;
using NexaFlow.Core.Enums;
using NexaFlow.Infrastructure.Persistence;
using NexaFlow.Infrastructure.Persistence.Interceptors;
using Xunit;

namespace NexaFlow.Tests.Infrastructure.Persistence;

public class AuditInterceptorTests
{
    private readonly AppDbContext _db;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public AuditInterceptorTests()
    {
        var tenantContextMock = new Mock<ITenantContext>();
        tenantContextMock.Setup(x => x.TenantId).Returns(_tenantId);

        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.Setup(x => x.UserId).Returns(_userId);

        var auditInterceptor = new AuditInterceptor(tenantContextMock.Object, currentUserMock.Object);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(auditInterceptor)
            .Options;

        _db = new AppDbContext(options, tenantContextMock.Object);
    }

    [Fact]
    public async Task SaveChanges_ShouldCreateAuditLog_ForTenantEntity()
    {
        // Arrange
        var account = new Account
        {
            Code = "1001",
            Name = "Bank",
            Type = AccountType.Asset,
            TenantId = _tenantId
        };
        _db.Accounts.Add(account);

        // Act
        await _db.SaveChangesAsync();

        // Assert
        var auditLogs = await _db.AuditLogs.ToListAsync();
        
        foreach (var al in auditLogs) {
            Console.WriteLine($"AuditLog: {al.EntityName} {al.Action} - {al.EntityId}");
        }
        foreach (var entry in _db.ChangeTracker.Entries()) {
            Console.WriteLine($"ChangeTracker: {entry.Entity.GetType().Name} {entry.State}");
        }

        auditLogs.Should().NotBeEmpty();
        
        var log = auditLogs.First(a => a.EntityName.Contains("Account") && a.Action == "Added");
        log.Should().NotBeNull();
        log.TenantId.Should().Be(_tenantId);
        log.UserId.Should().Be(_userId);
        log.NewValues.Should().Contain("Bank");
    }
}
