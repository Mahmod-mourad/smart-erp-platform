using NexaFlow.API.Infrastructure;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;

namespace NexaFlow.API.Endpoints;

public static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/chat").WithTags("Chat")
            .RequireAuthorization();

        group.MapPost("/", async (ChatRequest req, IChatbotService chatbot, CancellationToken ct) =>
            Results.Ok(await chatbot.ChatAsync(req, ct)))
            .AddEndpointFilter<ValidationFilter<ChatRequest>>()
            .WithSummary("Ask the AI assistant a question grounded in the tenant's business data.");

        group.MapGet("/status", async (IHttpClientFactory factory, CancellationToken ct) =>
        {
            try
            {
                var http = factory.CreateClient(nameof(IChatbotService));
                using var res = await http.GetAsync("/api/tags", ct);
                return Results.Ok(new { available = res.IsSuccessStatusCode });
            }
            catch
            {
                return Results.Ok(new { available = false });
            }
        })
            .WithSummary("Report whether the local Ollama server is reachable.");

        return app;
    }
}
