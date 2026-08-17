using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HelpDesk.Api.Application.Ai;
using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Configuration;
using Microsoft.Extensions.Options;

namespace HelpDesk.Api.Infrastructure.Ai;

public sealed class OpenAiTicketProvider(HttpClient http, IOptions<AiOptions> options)
{
    private readonly AiOptions _settings = options.Value;

    public async Task<AiProviderResult> AnalyzeAsync(
        AiTicketInput input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            throw new AiServiceUnavailableException();

        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        request.Content = JsonContent.Create(new
        {
            model = _settings.Model,
            input = new[]
            {
                new { role = "developer", content = AiTicketPrompt.SystemInstructions },
                new { role = "user", content = AiTicketPrompt.BuildUserContent(input) }
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "ticket_analysis",
                    strict = true,
                    schema = new
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
                }
            },
            max_output_tokens = 1400
        });

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AiServiceUnavailableException();
        }
        catch (HttpRequestException)
        {
            throw new AiProviderException();
        }

        using (response)
        {
            if (response.StatusCode is System.Net.HttpStatusCode.TooManyRequests or
                System.Net.HttpStatusCode.ServiceUnavailable)
                throw new AiServiceUnavailableException();
            if (!response.IsSuccessStatusCode)
                throw new AiProviderException();

            try
            {
                using var json = await JsonDocument.ParseAsync(
                    await response.Content.ReadAsStreamAsync(cancellationToken),
                    cancellationToken: cancellationToken);
                var output = json.RootElement.GetProperty("output").EnumerateArray()
                    .SelectMany(item => item.GetProperty("content").EnumerateArray())
                    .First(item => item.GetProperty("type").GetString() == "output_text")
                    .GetProperty("text").GetString();
                return ParseResult(output);
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

    private static AiProviderResult ParseResult(string? content)
    {
        var result = JsonSerializer.Deserialize<AiProviderResult>(
            content ?? string.Empty,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return result is not null && result.TroubleshootingSuggestions.Count is >= 3 and <= 5
            ? result
            : throw new AiProviderException();
    }
}
