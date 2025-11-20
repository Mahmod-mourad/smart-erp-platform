using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;
using NexaFlow.Core.Enums;
using NexaFlow.Infrastructure.Integrations;
using NexaFlow.Infrastructure.Persistence;

namespace NexaFlow.Tests;

public class IntegrationServiceTests
{
    // ---- Fakes ----

    // Identity byte-protector: the string Protect/Unprotect extensions still base64-encode around it,
    // so stored ciphertext differs from the plaintext while remaining reversible — enough to exercise
    // the encrypt/decrypt round-trip without pulling in the full Data Protection stack.
    private sealed class FakeProtector : IDataProtector
    {
        public IDataProtector CreateProtector(string purpose) => this;
        public byte[] Protect(byte[] plaintext) => plaintext;
        public byte[] Unprotect(byte[] protectedData) => protectedData;
    }

    private sealed class FakeDataProtectionProvider : IDataProtectionProvider
    {
        public IDataProtector CreateProtector(string purpose) => new FakeProtector();
    }

    private sealed class RecordingSlackSender : ISlackSender
    {
        public List<string> Sent { get; } = [];
        public Task SendAsync(string message, string? channel, CancellationToken ct = default)
        {
            Sent.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingEmailSender : IEmailSender
    {
        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
            => throw new InvalidOperationException("smtp down");
    }

    private sealed class NoopSheetsSender : IGoogleSheetsSender
    {
        public Task AppendRowAsync(IEnumerable<string> values, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    private sealed record Sut(
        IntegrationService Service,
        IntegrationConfigProvider ConfigProvider,
        AppDbContext Db,
        RecordingSlackSender Slack);

    private static Sut BuildSut(out TestHarness harness)
    {
        harness = new TestHarness();
        var tenantId = Guid.NewGuid();
        harness.TenantContext.SetTenant(tenantId);
        harness.CurrentUser.TenantId = tenantId;
        harness.CurrentUser.UserId = Guid.NewGuid();
        harness.CurrentUser.Email = "admin@acme.test";

        var db = harness.Get<AppDbContext>();
        var crypto = new IntegrationCrypto(new FakeDataProtectionProvider());
        var slack = new RecordingSlackSender();

        var service = new IntegrationService(
            db, harness.CurrentUser, crypto, slack, new ThrowingEmailSender(),
            new NoopSheetsSender(), new FakeHttpClientFactory(),
            NullLogger<IntegrationService>.Instance);

        var provider = new IntegrationConfigProvider(db, crypto, NullLogger<IntegrationConfigProvider>.Instance);

        return new Sut(service, provider, db, slack);
    }

    [Fact]
    public async Task Upsert_then_GetConfig_round_trips_the_credentials_and_stores_ciphertext()
    {
        var sut = BuildSut(out _);
        await sut.Service.UpsertAsync(IntegrationType.WhatsApp, new UpsertIntegrationDto(
            IsEnabled: true,
            Config: new() { ["accessToken"] = "tok-123", ["phoneNumberId"] = "pn-456" }));

        var config = await sut.ConfigProvider.GetConfigAsync<WhatsAppConfig>(IntegrationType.WhatsApp);
        config.Should().NotBeNull();
        config!.AccessToken.Should().Be("tok-123");
        config.PhoneNumberId.Should().Be("pn-456");

        var row = await sut.Db.TenantIntegrations.SingleAsync(i => i.Type == IntegrationType.WhatsApp);
        row.EncryptedConfig.Should().NotContain("tok-123", "credentials must not be stored in plaintext");
    }

    [Fact]
    public async Task Upsert_keeps_existing_secret_when_field_is_blank()
    {
        var sut = BuildSut(out _);
        await sut.Service.UpsertAsync(IntegrationType.Slack, new UpsertIntegrationDto(
            true, new() { ["webhookUrl"] = "https://hooks.slack.com/abc" }));

        // Re-save toggling enabled without resending the secret (blank value).
        await sut.Service.UpsertAsync(IntegrationType.Slack, new UpsertIntegrationDto(
            true, new() { ["webhookUrl"] = "" }));

        var config = await sut.ConfigProvider.GetConfigAsync<SlackConfig>(IntegrationType.Slack);
        config!.WebhookUrl.Should().Be("https://hooks.slack.com/abc");
    }

    [Fact]
    public async Task GetConfig_returns_null_when_integration_is_disabled()
    {
        var sut = BuildSut(out _);
        await sut.Service.UpsertAsync(IntegrationType.Slack, new UpsertIntegrationDto(
            IsEnabled: false, new() { ["webhookUrl"] = "https://hooks.slack.com/abc" }));

        var config = await sut.ConfigProvider.GetConfigAsync<SlackConfig>(IntegrationType.Slack);
        config.Should().BeNull();
    }

    [Fact]
    public async Task GetAll_lists_every_type_without_exposing_secrets()
    {
        var sut = BuildSut(out _);
        await sut.Service.UpsertAsync(IntegrationType.Slack, new UpsertIntegrationDto(
            true, new() { ["webhookUrl"] = "https://hooks.slack.com/abc" }));

        var all = await sut.Service.GetAllAsync();

        all.Should().HaveCount(Enum.GetValues<IntegrationType>().Length);
        var slack = all.Single(i => i.Type == nameof(IntegrationType.Slack));
        slack.IsConfigured.Should().BeTrue();
        slack.IsEnabled.Should().BeTrue();
        // IntegrationDto carries no credential fields at all — secrets cannot leak through this contract.
    }

    [Fact]
    public async Task Test_records_outcome_on_success()
    {
        var sut = BuildSut(out _);
        await sut.Service.UpsertAsync(IntegrationType.Slack, new UpsertIntegrationDto(
            true, new() { ["webhookUrl"] = "https://hooks.slack.com/abc" }));

        var result = await sut.Service.TestAsync(IntegrationType.Slack);

        result.Success.Should().BeTrue();
        sut.Slack.Sent.Should().ContainSingle();
        var row = await sut.Db.TenantIntegrations.SingleAsync(i => i.Type == IntegrationType.Slack);
        row.LastTestSuccess.Should().BeTrue();
        row.LastTestedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Test_records_failure_when_sender_throws()
    {
        var sut = BuildSut(out _);
        await sut.Service.UpsertAsync(IntegrationType.Gmail, new UpsertIntegrationDto(
            true, new() { ["email"] = "x@y.com", ["appPassword"] = "pw" }));

        var result = await sut.Service.TestAsync(IntegrationType.Gmail);

        result.Success.Should().BeFalse();
        var row = await sut.Db.TenantIntegrations.SingleAsync(i => i.Type == IntegrationType.Gmail);
        row.LastTestSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Test_fails_cleanly_when_not_configured()
    {
        var sut = BuildSut(out _);
        var result = await sut.Service.TestAsync(IntegrationType.Slack);
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not configured");
    }
}
