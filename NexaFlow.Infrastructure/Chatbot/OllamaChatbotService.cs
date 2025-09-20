using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.DTOs;

namespace NexaFlow.Infrastructure.Chatbot;

/// <summary>
/// Chatbot backed by a locally-running Ollama model. Grounds each conversation in the tenant's
/// business data (see <see cref="ChatContextBuilder"/>) and fails soft — any error maps to a
/// friendly, unsuccessful <see cref="ChatResponse"/> rather than throwing to the endpoint.
/// </summary>
public class OllamaChatbotService(
    HttpClient http,
    ChatContextBuilder contextBuilder,
    IConfiguration config,
    ILogger<OllamaChatbotService> logger) : IChatbotService
{
    private readonly string _model = config["OllamaSettings:Model"] ?? "llama3.2";

    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct = default)
    {
        try
        {
            var systemContext = await contextBuilder.BuildContextAsync(ct);

            var messages = new List<object> { new { role = "system", content = systemContext } };
            foreach (var msg in request.History.TakeLast(10))
                messages.Add(new { role = msg.Role, content = msg.Content });
            messages.Add(new { role = "user", content = request.Message });

            var payload = new
            {
                model = _model,
                messages,
                stream = false,
                options = new { temperature = 0.7, num_predict = 250 }
            };

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            var response = await http.PostAsJsonAsync("/api/chat", payload, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Ollama returned {Status}", response.StatusCode);
                return Fail("Sorry, the AI assistant is unavailable right now. Please try again.", "Ollama API error");
            }

            var result = await response.Content.ReadFromJsonAsync<OllamaResponse>(ct);
            var text = result?.Message?.Content?.Trim();
            text = string.IsNullOrEmpty(text) ? "I didn't quite get that. Could you rephrase?" : text;

            return new ChatResponse(text, DateTime.UtcNow, true, null);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return Fail("The assistant took too long to respond. Please try again.", "Timeout");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ollama chatbot error");
            return Fail("Something went wrong. Make sure Ollama is running locally.", ex.Message);
        }
    }

    private static ChatResponse Fail(string message, string error) => new(message, DateTime.UtcNow, false, error);

    private sealed record OllamaResponse([property: JsonPropertyName("message")] OllamaMessage? Message);
    private sealed record OllamaMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);
}
