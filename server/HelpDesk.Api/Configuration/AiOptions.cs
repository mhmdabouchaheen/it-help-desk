namespace HelpDesk.Api.Configuration;

/// <summary>Configures the optional ticket-analysis provider.</summary>
public sealed class AiOptions
{
    public const string SectionName = "Ai";
    public const string OpenAiProvider = "OpenAI";
    public const string OllamaProvider = "Ollama";

    public string Provider { get; init; } = OpenAiProvider;
    public string Model { get; init; } = "gpt-5.4-mini";
    public string ApiKey { get; init; } = string.Empty;
    public string Endpoint { get; init; } = "https://api.openai.com/v1/responses";
    public string OllamaModel { get; init; } = "llama3.2:3b";
    public string OllamaEndpoint { get; init; } = "http://localhost:11434/api/chat";
    public int TimeoutSeconds { get; init; } = 60;
}
