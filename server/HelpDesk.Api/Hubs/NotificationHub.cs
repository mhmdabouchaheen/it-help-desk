using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HelpDesk.Api.Hubs;

/// <summary>Authenticated, server-to-client notification invalidation hub.</summary>
[Authorize]
public sealed class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var principal = Context.User ?? throw new HubException("Invalid authenticated principal.");
        string group;
        try { group = NotificationUserGroup.FromPrincipal(principal); }
        catch (Exception exception) when (exception is ArgumentException or Application.Common.Exceptions.InvalidAuthenticatedPrincipalException)
        { throw new HubException("Invalid authenticated principal."); }
        await Groups.AddToGroupAsync(Context.ConnectionId, group, Context.ConnectionAborted);
        await base.OnConnectedAsync();
    }
}
