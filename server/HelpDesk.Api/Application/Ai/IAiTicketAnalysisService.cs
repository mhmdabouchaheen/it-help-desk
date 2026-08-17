using HelpDesk.Api.Application.Tickets;using HelpDesk.Api.Contracts.Ai;
namespace HelpDesk.Api.Application.Ai;
public interface IAiTicketAnalysisService{Task<AiTicketAnalysisResponse>AnalyzeTicketAsync(Guid ticketId,TicketAccessContext accessContext,CancellationToken cancellationToken=default);}
public sealed record AiTicketInput(string Title,string Description,IReadOnlyList<string>Categories,IReadOnlyList<string>Priorities);
public sealed record AiProviderResult(string Summary,string?RecommendedCategoryName,string?RecommendedPriorityName,IReadOnlyList<string>TroubleshootingSuggestions);
public interface IAiTicketProvider{Task<AiProviderResult>AnalyzeAsync(AiTicketInput input,CancellationToken cancellationToken=default);}
