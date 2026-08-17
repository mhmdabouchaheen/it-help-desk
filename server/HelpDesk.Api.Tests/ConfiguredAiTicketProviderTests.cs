using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Configuration;
using HelpDesk.Api.Infrastructure.Ai;
using Microsoft.Extensions.Options;

namespace HelpDesk.Api.Tests;

public sealed class ConfiguredAiTicketProviderTests
{
    [Fact]
    public async Task OllamaSelectionDoesNotRequireOrCallOpenAi()
    {
        var openAiHandler = new OllamaTicketProviderTests.Handler(_ =>
            throw new InvalidOperationException("OpenAI must not run"));
        var ollamaHandler = new OllamaTicketProviderTests.Handler(_ =>
            OllamaTicketProviderTests.Response("Local", null, null, []));
        var options = Options.Create(new AiOptions
        {
            Provider = "ollama",
            OllamaModel = "llama3.2:3b",
            OllamaEndpoint = "http://localhost:11434/api/chat"
        });
        var provider = new ConfiguredAiTicketProvider(
            options,
            new OpenAiTicketProvider(new HttpClient(openAiHandler), options),
            new OllamaTicketProvider(new HttpClient(ollamaHandler), options));

        var result = await provider.AnalyzeAsync(OllamaTicketProviderTests.Input());

        Assert.Equal("Local", result.Summary);
        Assert.Equal(0, openAiHandler.Calls);
        Assert.Equal(1, ollamaHandler.Calls);
    }

    [Fact]
    public async Task UnknownProviderFailsWithoutCallingEitherNetwork()
    {
        var openAiHandler = new OllamaTicketProviderTests.Handler(_ =>
            throw new InvalidOperationException("network must not run"));
        var ollamaHandler = new OllamaTicketProviderTests.Handler(_ =>
            throw new InvalidOperationException("network must not run"));
        var options = Options.Create(new AiOptions { Provider = "unknown" });
        var provider = new ConfiguredAiTicketProvider(
            options,
            new OpenAiTicketProvider(new HttpClient(openAiHandler), options),
            new OllamaTicketProvider(new HttpClient(ollamaHandler), options));

        await Assert.ThrowsAsync<AiServiceUnavailableException>(() =>
            provider.AnalyzeAsync(OllamaTicketProviderTests.Input()));
        Assert.Equal(0, openAiHandler.Calls);
        Assert.Equal(0, ollamaHandler.Calls);
    }
}
