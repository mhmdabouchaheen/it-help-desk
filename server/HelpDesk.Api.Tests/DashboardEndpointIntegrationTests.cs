using System.Net;
using System.Text.Json;
using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Contracts.Dashboard;
using Moq;

namespace HelpDesk.Api.Tests;

public sealed class DashboardEndpointIntegrationTests
{
    [Fact]
    public async Task AnonymousRequest_IsRejectedWithoutCallingDashboardService()
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateClient().GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        factory.DashboardService.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.ItSupportAgent)]
    [InlineData(AppRoles.Employee)]
    [InlineData(AppRoles.Manager)]
    public async Task JwtSubjectAndRoles_AreForwardedToTheService(string role)
    {
        await using var factory = new AuthApiFactory();
        var userId = Guid.NewGuid();
        var response = await factory.CreateAuthorizedClient(userId, role).GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        factory.DashboardService.Verify(x => x.GetDashboardAsync(
            It.Is<TicketAccessContext>(a => a.UserId == userId && a.Roles.SequenceEqual(new[] { role })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SupportRoleIsAdditive_AndRequestOverridesAreIgnored()
    {
        await using var factory = new AuthApiFactory();
        var subject = Guid.NewGuid();
        var attacker = Guid.NewGuid();
        var client = factory.CreateAuthorizedClient(subject, AppRoles.Employee, AppRoles.ItSupportAgent);
        client.DefaultRequestHeaders.Add("X-User-Id", attacker.ToString());
        client.DefaultRequestHeaders.Add("X-Roles", AppRoles.Admin);
        var response = await client.GetAsync($"/api/dashboard?userId={attacker}&roles={Uri.EscapeDataString(AppRoles.Admin)}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        factory.DashboardService.Verify(x => x.GetDashboardAsync(It.Is<TicketAccessContext>(a =>
            a.UserId == subject && a.Roles.Count == 2 && a.Roles.Contains(AppRoles.Employee) &&
            a.Roles.Contains(AppRoles.ItSupportAgent) && !a.Roles.Contains(AppRoles.Admin)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResponseContainsOnlyDashboardContractFields()
    {
        await using var factory = new AuthApiFactory();
        factory.DashboardService.Setup(x => x.GetDashboardAsync(It.IsAny<TicketAccessContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardResponse
            {
                RecentTickets = [new DashboardRecentTicketResponse
                {
                    Id = Guid.NewGuid(), ReferenceNumber = "TKT-1", Title = "Safe", StatusName = "Open",
                    PriorityName = "Low", CategoryName = "Hardware", UpdatedAtUtc = DateTime.UtcNow
                }]
            });
        var response = await factory.CreateAuthorizedClient(Guid.NewGuid(), AppRoles.Employee).GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = (await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync())).RootElement;
        Assert.Equal(new[] { "summary", "statusBreakdown", "priorityBreakdown", "categoryBreakdown", "monthlyTrend", "recentTickets" },
            json.EnumerateObject().Select(x => x.Name).ToArray());
        Assert.Equal(new[] { "id", "referenceNumber", "title", "statusName", "priorityName", "categoryName", "createdAtUtc", "updatedAtUtc", "cancelledAtUtc", "assignedToDisplayName" },
            json.GetProperty("recentTickets")[0].EnumerateObject().Select(x => x.Name).ToArray());
        var text = json.GetRawText();
        Assert.DoesNotContain("email", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("accessToken", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", text, StringComparison.OrdinalIgnoreCase);
    }
}
