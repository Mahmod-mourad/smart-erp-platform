using NexaFlow.Application.Common.Interfaces;

namespace NexaFlow.API.Endpoints;

public static class PredictionEndpoints
{
    public static IEndpointRouteBuilder MapPredictionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/predictions").WithTags("Predictions")
            .RequireAuthorization();

        group.MapGet("/sales-forecast", async (IPredictionService svc, int monthsAhead = 3) =>
            Results.Ok(await svc.ForecastSalesAsync(monthsAhead)))
            .WithSummary("Forecast monthly won-sales for the next N months (SSA time-series).");

        group.MapGet("/churn-risk", async (IPredictionService svc) =>
            Results.Ok(await svc.GetChurnRiskAsync()))
            .WithSummary("Top customers by churn probability, highest risk first (FastTree).");

        group.MapGet("/stock-depletion", async (IPredictionService svc) =>
            Results.Ok(await svc.GetStockDepletionAsync()))
            .WithSummary("Products by stock runway, most urgent first.");

        group.MapGet("/dashboard-summary", async (IPredictionService svc) =>
            Results.Ok(await svc.GetDashboardSummaryAsync()))
            .WithSummary("All predictions plus headline KPI counts in a single request.");

        return app;
    }
}
