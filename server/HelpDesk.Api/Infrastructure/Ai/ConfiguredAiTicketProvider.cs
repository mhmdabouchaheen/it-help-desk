using HelpDesk.Api.Application.Ai;
using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Configuration;
using Microsoft.Extensions.Options;

namespace HelpDesk.Api.Infrastructure.Ai;

/// <summary>Selects the explicitly configured ticket-analysis provider.</summary>
public sealed class ConfiguredAiTicketProvider(
    IOptions<AiOptions> options,
    OpenAiTicketProvider openAi,
    OllamaTicketProvider ollama) : IAiTicketProvider
{
    public Task<AiProviderResult> AnalyzeAsync(
        AiTicketInput input,
        CancellationToken cancellationToken = default) =>
        options.Value.Provider.Trim().ToUpperInvariant() switch
        {
            "OPENAI" => openAi.AnalyzeAsync(input, cancellationToken),
            "OLLAMA" => ollama.AnalyzeAsync(input, cancellationToken),
            _ => throw new AiServiceUnavailableException()
        };
}
