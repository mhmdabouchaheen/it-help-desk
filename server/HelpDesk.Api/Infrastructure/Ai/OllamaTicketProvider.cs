using System.Net.Http.Json;
using System.Text.Json;
using HelpDesk.Api.Application.Ai;
using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Configuration;
using Microsoft.Extensions.Options;

namespace HelpDesk.Api.Infrastructure.Ai;

/// <summary>Uses a local Ollama model to produce structured advisory ticket analysis.</summary>
public sealed class OllamaTicketProvider(HttpClient http, IOptions<AiOptions> options)
{
    private readonly AiOptions _settings = options.Value;

    public async Task<AiProviderResult> AnalyzeAsync(
        AiTicketInput input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.OllamaModel) ||
            !Uri.TryCreate(_settings.OllamaEndpoint, UriKind.Absolute, out var endpoint) ||
            endpoint.Scheme is not ("http" or "https"))
            throw new AiServiceUnavailableException();

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(new
            {
                model = _settings.OllamaModel,
                stream = false,
                think = false,
                options = new { temperature = 0 },
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = AiTicketPrompt.SystemInstructions
                    },
                    new
                    {
                        role = "user",
                        content = AiTicketPrompt.BuildUserContent(input)
                    }
                },
                format = new
                {
                    type = "object",
                    properties = new
                    {
                        summary = new
                        {
                            type = "string",
                            description = "A factual 2-4 sentence summary of the issue, affected capability, and known impact."
                        },
                        recommendedCategoryName = new { type = new[] { "string", "null" } },
                        recommendedPriorityName = new { type = new[] { "string", "null" } },
                        troubleshootingSuggestions = new
                        {
                            type = "array",
                            description = "Three to five ordered, safe, actionable diagnostic and verification steps.",
                            items = new { type = "string" },
                            minItems = 3,
                            maxItems = 5
                        }
                    },
                    required = new[]
                    {
                        "summary",
                        "recommendedCategoryName",
                        "recommendedPriorityName",
                        "troubleshootingSuggestions"
                    },
                    additionalProperties = false
                }
            })
        };

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AiServiceUnavailableException();
        }
        catch (HttpRequestException)
        {
            throw new AiServiceUnavailableException();
        }

        using (response)
        {
            if (response.StatusCode is System.Net.HttpStatusCode.NotFound or
                System.Net.HttpStatusCode.ServiceUnavailable or
                System.Net.HttpStatusCode.TooManyRequests)
                throw new AiServiceUnavailableException();
            if (!response.IsSuccessStatusCode)
                throw new AiProviderException();

            try
            {
                using var json = await JsonDocument.ParseAsync(
                    await response.Content.ReadAsStreamAsync(cancellationToken),
                    cancellationToken: cancellationToken);
                var content = json.RootElement.GetProperty("message").GetProperty("content").GetString();
                var result = JsonSerializer.Deserialize<AiProviderResult>(
                    content ?? string.Empty,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result is not null && result.TroubleshootingSuggestions.Count is >= 3 and <= 5
                    ? result
                    : throw new AiProviderException();
            }
            catch (JsonException)
            {
                throw new AiProviderException();
            }
            catch (InvalidOperationException)
            {
                throw new AiProviderException();
            }
            catch (KeyNotFoundException)
            {
                throw new AiProviderException();
            }
        }
    }
}
