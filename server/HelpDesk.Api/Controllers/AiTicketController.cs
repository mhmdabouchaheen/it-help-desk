using HelpDesk.Api.Application.Ai;using HelpDesk.Api.Application.Authorization;using HelpDesk.Api.Contracts.Ai;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;
namespace HelpDesk.Api.Controllers;
[ApiController][Route("api/tickets/{ticketId:guid}/ai-analysis")][Authorize]
public sealed class AiTicketController(IAiTicketAnalysisService analysis,ITicketAccessContextFactory access):ControllerBase
{[HttpPost][ProducesResponseType(typeof(AiTicketAnalysisResponse),StatusCodes.Status200OK)][ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status503ServiceUnavailable)]public async Task<ActionResult<AiTicketAnalysisResponse>>AnalyzeAsync(Guid ticketId,CancellationToken cancellationToken)=>Ok(await analysis.AnalyzeTicketAsync(ticketId,access.Create(User),cancellationToken));}
