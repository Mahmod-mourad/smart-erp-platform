using FluentAssertions;
using NexaFlow.Application.Common.Exceptions;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Constants;

namespace NexaFlow.Tests;

public class AuthServiceTests
{
    private static RegisterCompanyRequest ValidCompany(string email = "admin@acme.com") =>
        new("Acme Corp", "Ada", "Lovelace", email, "Sup3rSecret!");

    [Fact]
    public async Task RegisterCompany_creates_tenant_admin_and_returns_tokens()
    {
        var h = new TestHarness();
        await h.SeedRolesAsync();
        var auth = h.Get<IAuthService>();

        var result = await auth.RegisterCompanyAsync(ValidCompany(), "127.0.0.1");

        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.User.Email.Should().Be("admin@acme.com");
        result.User.TenantId.Should().NotBeEmpty();
        result.User.Roles.Should().Contain(AppRoles.CompanyAdmin);
    }

    [Fact]
    public async Task RegisterCompany_with_duplicate_email_throws_Conflict()
    {
        var h = new TestHarness();
        await h.SeedRolesAsync();
        var auth = h.Get<IAuthService>();

        await auth.RegisterCompanyAsync(ValidCompany(), null);

        var act = async () => await auth.RegisterCompanyAsync(ValidCompany(), null);
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Login_with_correct_password_succeeds()
    {
        var h = new TestHarness();
        await h.SeedRolesAsync();
        var auth = h.Get<IAuthService>();
        await auth.RegisterCompanyAsync(ValidCompany(), null);
        h.TenantContext.Clear(); // simulate a fresh anonymous login request

        var result = await auth.LoginAsync(new LoginRequest("admin@acme.com", "Sup3rSecret!"), null);

        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.User.Email.Should().Be("admin@acme.com");
    }

    [Fact]
    public async Task Login_with_wrong_password_throws_Unauthorized()
    {
        var h = new TestHarness();
        await h.SeedRolesAsync();
        var auth = h.Get<IAuthService>();
        await auth.RegisterCompanyAsync(ValidCompany(), null);

        var act = async () => await auth.LoginAsync(new LoginRequest("admin@acme.com", "wrong"), null);
        await act.Should().ThrowAsync<UnauthorizedAppException>();
    }

    [Fact]
    public async Task Login_unknown_email_throws_Unauthorized()
    {
        var h = new TestHarness();
        await h.SeedRolesAsync();
        var auth = h.Get<IAuthService>();

        var act = async () => await auth.LoginAsync(new LoginRequest("nobody@acme.com", "whatever"), null);
        await act.Should().ThrowAsync<UnauthorizedAppException>();
    }

    [Fact]
    public async Task RefreshToken_rotates_and_old_token_becomes_invalid()
    {
        var h = new TestHarness();
        await h.SeedRolesAsync();
        var auth = h.Get<IAuthService>();
        var login = await auth.RegisterCompanyAsync(ValidCompany(), null);

        var refreshed = await auth.RefreshTokenAsync(login.RefreshToken, null);
        refreshed.RefreshToken.Should().NotBe(login.RefreshToken);

        // Re-using the rotated token must fail.
        var act = async () => await auth.RefreshTokenAsync(login.RefreshToken, null);
        await act.Should().ThrowAsync<UnauthorizedAppException>();
    }
}
