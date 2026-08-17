namespace HelpDesk.Api.Application.Common.Exceptions;
public sealed class AiServiceUnavailableException:Exception{public AiServiceUnavailableException():base("AI analysis is temporarily unavailable."){}}
public sealed class AiProviderException:Exception{public AiProviderException():base("AI analysis could not be completed."){}}
