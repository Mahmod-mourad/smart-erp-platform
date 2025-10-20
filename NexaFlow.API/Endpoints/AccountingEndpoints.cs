using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.Common.Security;
using NexaFlow.Application.DTOs;

namespace NexaFlow.API.Endpoints;

public static class AccountingEndpoints
{
    public static void MapAccountingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/accounting")
            .RequireAuthorization(AppPermissions.Finance.View)
            .WithTags("Accounting");

        group.MapGet("/accounts", async (IAccountingService service, CancellationToken ct) =>
            Results.Ok(await service.GetAccountsAsync(ct)));

        group.MapPost("/accounts", async (CreateAccountDto request, IValidator<CreateAccountDto> validator, IAccountingService service, CancellationToken ct) =>
        {
            var val = await validator.ValidateAsync(request, ct);
            if (!val.IsValid) return Results.ValidationProblem(val.ToDictionary());

            var account = await service.CreateAccountAsync(request, ct);
            return Results.Created($"/api/accounting/accounts/{account.Id}", account);
        })
        .RequireAuthorization(AppPermissions.Finance.ManageAccounts);

        group.MapGet("/journals", async (IAccountingService service, CancellationToken ct) =>
            Results.Ok(await service.GetJournalEntriesAsync(ct)));

        group.MapPost("/journals", async (CreateJournalEntryDto request, IValidator<CreateJournalEntryDto> validator, IAccountingService service, CancellationToken ct) =>
        {
            var val = await validator.ValidateAsync(request, ct);
            if (!val.IsValid) return Results.ValidationProblem(val.ToDictionary());

            var journal = await service.PostJournalEntryAsync(request, ct);
            return Results.Created($"/api/accounting/journals/{journal.Id}", journal);
        })
        .RequireAuthorization(AppPermissions.Finance.PostJournals);
    }
}
