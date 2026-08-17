namespace HelpDesk.Api.Contracts.Ai;
public sealed class AiTicketAnalysisResponse
{public required string Summary{get;init;}public short?RecommendedCategoryId{get;init;}public string?RecommendedCategoryName{get;init;}public short?RecommendedPriorityId{get;init;}public string?RecommendedPriorityName{get;init;}public IReadOnlyList<string>TroubleshootingSuggestions{get;init;}=[];public string Disclaimer{get;init;}="AI-generated suggestions may be inaccurate. Review before applying.";}
