namespace NexaFlow.Application.DTOs;

/// <summary>A single turn in the conversation. Role is "user" or "assistant".</summary>
public record ChatMessage(string Role, string Content);

/// <summary>A chat request from the client: the new message plus prior turns for context.</summary>
public record ChatRequest(string Message, List<ChatMessage> History);

/// <summary>The assistant's reply. On failure, <see cref="Success"/> is false and the message is user-friendly.</summary>
public record ChatResponse(
    string Message,
    DateTime Timestamp,
    bool Success,
    string? ErrorMessage);
