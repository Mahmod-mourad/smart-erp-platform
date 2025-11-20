using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Enums;
using NexaFlow.Infrastructure.Persistence;
using NexaFlow.Infrastructure.Services;
using Xunit;

namespace NexaFlow.Tests.Infrastructure.Services;

public class AccountingServiceTests
{
    private readonly AppDbContext _db;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly AccountingService _sut;

    public AccountingServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantContextMock = new Mock<ITenantContext>();
        tenantContextMock.Setup(x => x.TenantId).Returns(Guid.NewGuid());
        
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(x => x.UserId).Returns(Guid.NewGuid());

        _db = new AppDbContext(options, tenantContextMock.Object);
        _sut = new AccountingService(_db, _currentUserMock.Object);
    }

    [Fact]
    public async Task CreateAccount_ShouldSaveAccount()
    {
        // Arrange
        var req = new CreateAccountDto("1000", "Cash", AccountType.Asset, null);

        // Act
        var result = await _sut.CreateAccountAsync(req);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be("1000");
        result.Balance.Should().Be(0);

        var dbAccount = await _db.Accounts.FirstOrDefaultAsync();
        dbAccount.Should().NotBeNull();
        dbAccount!.Code.Should().Be("1000");
    }

    [Fact]
    public async Task PostJournalEntry_ShouldUpdateAccountBalances()
    {
        // Arrange
        var cashAcc = await _sut.CreateAccountAsync(new CreateAccountDto("1000", "Cash", AccountType.Asset, null));
        var revenueAcc = await _sut.CreateAccountAsync(new CreateAccountDto("4000", "Sales", AccountType.Revenue, null));

        var req = new CreateJournalEntryDto("JE-001", DateTime.UtcNow, "Sale", new List<JournalEntryLineDto>
        {
            new JournalEntryLineDto(cashAcc.Id, 1000m, 0, null),
            new JournalEntryLineDto(revenueAcc.Id, 0, 1000m, null)
        });

        // Act
        var result = await _sut.PostJournalEntryAsync(req);

        // Assert
        result.Should().NotBeNull();
        result.ReferenceNumber.Should().Be("JE-001");

        var dbCash = await _db.Accounts.FindAsync([cashAcc.Id]);
        var dbRev = await _db.Accounts.FindAsync([revenueAcc.Id]);

        dbCash!.Balance.Should().Be(1000m); // Asset: +Debit
        dbRev!.Balance.Should().Be(1000m);  // Revenue: +Credit
    }
}
