using HelpDesk.Api.Application.Ai;
using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Contracts.Ai;

namespace HelpDesk.Api.Infrastructure.Ai;

public sealed class AiTicketAnalysisService(
    ITicketService tickets,
    ITicketLookupService lookups,
    IAiTicketProvider provider) : IAiTicketAnalysisService
{
    public async Task<AiTicketAnalysisResponse> AnalyzeTicketAsync(
        Guid ticketId,
        TicketAccessContext access,
        CancellationToken cancellationToken = default)
    {
        if (ticketId == Guid.Empty)
            throw new TicketNotFoundException();
        if (access is null || access.UserId == Guid.Empty)
            throw new TicketAccessDeniedException();

        var ticket = await tickets.GetByIdAsync(ticketId, access, cancellationToken);
        var categories = (await lookups.GetCategoriesAsync(cancellationToken))
            .Where(item => item.IsActive).ToArray();
        var priorities = (await lookups.GetPrioritiesAsync(cancellationToken))
            .Where(item => item.IsActive).ToArray();
        var raw = await provider.AnalyzeAsync(
            new AiTicketInput(
                ticket.Title,
                ticket.Description,
                categories.Select(item => item.Name).ToArray(),
                priorities.Select(item => item.Name).ToArray()),
            cancellationToken);

        var category = categories.FirstOrDefault(item => string.Equals(
            item.Name,
            raw.RecommendedCategoryName?.Trim(),
            StringComparison.OrdinalIgnoreCase));
        var priority = priorities.FirstOrDefault(item => string.Equals(
            item.Name,
            raw.RecommendedPriorityName?.Trim(),
            StringComparison.OrdinalIgnoreCase));
        var suggestions = raw.TroubleshootingSuggestions
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Take(5)
            .Select(item => Bound(item, 500))
            .ToArray();
        if (suggestions.Length < 3)
            throw new AiProviderException();

        return new AiTicketAnalysisResponse
        {
            Summary = Bound(raw.Summary, 1000),
            RecommendedCategoryId = category?.Id,
            RecommendedCategoryName = category?.Name,
            RecommendedPriorityId = priority?.Id,
            RecommendedPriorityName = priority?.Name,
            TroubleshootingSuggestions = suggestions
        };
    }

    private static string Bound(string? value, int length)
    {
        var text = (value ?? string.Empty).Trim();
        return text.Length <= length ? text : text[..length];
    }
}
