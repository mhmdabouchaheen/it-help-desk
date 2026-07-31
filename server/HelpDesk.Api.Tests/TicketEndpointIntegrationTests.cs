using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Contracts.Common;
using HelpDesk.Api.Contracts.Tickets;
using Moq;

namespace HelpDesk.Api.Tests;

public class TicketEndpointIntegrationTests
{
    public static TheoryData<string, string> ProtectedRoutes => new()
    {
        { "POST", "/api/tickets" }, { "GET", "/api/tickets" },
        { "GET", $"/api/tickets/{AuthApiFactory.TicketId}" },
        { "PUT", $"/api/tickets/{AuthApiFactory.TicketId}" },
        { "POST", $"/api/tickets/{AuthApiFactory.TicketId}/assignment" },
        { "POST", $"/api/tickets/{AuthApiFactory.TicketId}/status" },
        { "POST", $"/api/tickets/{AuthApiFactory.TicketId}/comments" },
        { "GET", "/api/ticket-lookups/categories" },
        { "GET", "/api/ticket-lookups/priorities" },
        { "GET", "/api/ticket-lookups/statuses" }
    };

    [Theory, MemberData(nameof(ProtectedRoutes))]
    public async Task Routes_RequireAuthentication(string method, string route)
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateClient().SendAsync(new HttpRequestMessage(new HttpMethod(method), route)
        {
            Content = method is "POST" or "PUT" ? JsonContent.Create(new { }) : null
        });
        await AssertProblem(response, HttpStatusCode.Unauthorized, "authentication_required");
    }

    [Fact]
    public async Task Create_Returns201LocationAndJwtAccessContext()
    {
        var userId = Guid.NewGuid();
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateAuthorizedClient(userId, AppRoles.Employee, AppRoles.Manager)
            .PostAsJsonAsync("/api/tickets", ValidCreate());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/tickets/{AuthApiFactory.TicketId}", response.Headers.Location?.AbsolutePath);
        factory.TicketService.Verify(x => x.CreateAsync(
            It.IsAny<CreateTicketRequest>(),
            It.Is<TicketAccessContext>(a => a.UserId == userId && a.Roles.Count == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task List_BindsDefaultsAndFiltersWithoutOwnershipRewrite()
    {
        await using var factory = new AuthApiFactory();
        var creator = Guid.NewGuid();
        var response = await factory.CreateAuthorizedClient(Guid.NewGuid(), AppRoles.Employee)
            .GetAsync($"/api/tickets?search=printer&categoryId=1&pageNumber=2&pageSize=10&createdByUserId={creator}&sortBy=Title&sortDirection=asc");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        factory.TicketService.Verify(x => x.GetPagedAsync(
            It.Is<TicketListRequest>(r => r.Search == "printer" && r.CategoryId == 1 && r.PageNumber == 2 &&
                r.PageSize == 10 && r.CreatedByUserId == creator && r.SortBy == "Title" && r.SortDirection == "asc"),
            It.IsAny<TicketAccessContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("Employee", false)]
    [InlineData("Manager", false)]
    [InlineData("Unknown", false)]
    [InlineData("Admin", true)]
    [InlineData("IT Support Agent", true)]
    public async Task Assignment_SupportPolicyIsEnforced(string role, bool allowed)
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateAuthorizedClient(Guid.NewGuid(), role).PostAsJsonAsync(
            $"/api/tickets/{AuthApiFactory.TicketId}/assignment", new AssignTicketRequest { AssignedToUserId = Guid.NewGuid() });
        Assert.Equal(allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden, response.StatusCode);
        factory.TicketService.Verify(x => x.AssignAsync(It.IsAny<Guid>(), It.IsAny<AssignTicketRequest>(),
            It.IsAny<TicketAccessContext>(), It.IsAny<CancellationToken>()), allowed ? Times.Once() : Times.Never());
    }

    [Fact]
    public async Task MultiRoleSupportCanChangeStatus()
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateAuthorizedClient(Guid.NewGuid(), AppRoles.Employee, AppRoles.ItSupportAgent)
            .PostAsJsonAsync($"/api/tickets/{AuthApiFactory.TicketId}/status", new ChangeTicketStatusRequest { StatusId = 2, Note = "note" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        factory.TicketService.Verify(x => x.ChangeStatusAsync(AuthApiFactory.TicketId,
            It.Is<ChangeTicketStatusRequest>(r => r.StatusId == 2 && r.Note == "note"),
            It.IsAny<TicketAccessContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAndGet_ForwardRouteAndBody()
    {
        await using var factory = new AuthApiFactory();
        var client = factory.CreateAuthorizedClient(Guid.NewGuid(), AppRoles.Employee);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/tickets/{AuthApiFactory.TicketId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/tickets/{AuthApiFactory.TicketId}", ValidUpdate())).StatusCode);
        factory.TicketService.Verify(x => x.GetByIdAsync(AuthApiFactory.TicketId, It.IsAny<TicketAccessContext>(), It.IsAny<CancellationToken>()), Times.Once);
        factory.TicketService.Verify(x => x.UpdateAsync(AuthApiFactory.TicketId, It.Is<UpdateTicketRequest>(r => r.Title == "Updated"),
            It.IsAny<TicketAccessContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Comment_ReturnsStableCreatedLocationAndBindsBody()
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateAuthorizedClient(Guid.NewGuid(), AppRoles.Employee).PostAsJsonAsync(
            $"/api/tickets/{AuthApiFactory.TicketId}/comments", new AddTicketCommentRequest { Content = "hello", IsInternal = true });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/tickets/{AuthApiFactory.TicketId}/comments/{AuthApiFactory.CommentId}", response.Headers.Location?.OriginalString);
        factory.TicketService.Verify(x => x.AddCommentAsync(AuthApiFactory.TicketId,
            It.Is<AddTicketCommentRequest>(r => r.Content == "hello" && r.IsInternal),
            It.IsAny<TicketAccessContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("categories")]
    [InlineData("priorities")]
    [InlineData("statuses")]
    public async Task Lookups_AreAuthenticatedAndDelegate(string endpoint)
    {
        await using var factory = new AuthApiFactory();
        Assert.Equal(HttpStatusCode.OK, (await factory.CreateAuthorizedClient(Guid.NewGuid(), "Unknown")
            .GetAsync($"/api/ticket-lookups/{endpoint}")).StatusCode);
    }

    [Theory]
    [InlineData("not-found", 404, "ticket_not_found")]
    [InlineData("denied", 403, "ticket_access_denied")]
    [InlineData("validation", 400, "ticket_validation_failed")]
    [InlineData("conflict", 409, "ticket_state_conflict")]
    [InlineData("unexpected", 500, "internal_server_error")]
    public async Task ControlledExceptions_MapToSafeProblems(string kind, int status, string code)
    {
        await using var factory = new AuthApiFactory();
        Exception exception = kind switch
        {
            "not-found" => new TicketNotFoundException(), "denied" => new TicketAccessDeniedException(),
            "validation" => new TicketValidationException(), "conflict" => new TicketStateConflictException(),
            _ => new InvalidOperationException("database secret")
        };
        factory.TicketService.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<TicketAccessContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);
        var response = await factory.CreateAuthorizedClient(Guid.NewGuid(), AppRoles.Employee).GetAsync($"/api/tickets/{AuthApiFactory.TicketId}");
        await AssertProblem(response, (HttpStatusCode)status, code);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("database secret", body);
        Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("/api/tickets", "{}")]
    [InlineData("/api/tickets", "{\"title\":\"x\",\"description\":\"x\",\"categoryId\":0,\"priorityId\":0}")]
    public async Task InvalidCreate_ReturnsValidationProblem(string route, string json)
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateAuthorizedClient(Guid.NewGuid(), AppRoles.Employee).PostAsync(route,
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        await AssertProblem(response, HttpStatusCode.BadRequest, "validation_failed");
    }

    private static CreateTicketRequest ValidCreate() => new() { Title = "Test", Description = "Description", CategoryId = 1, PriorityId = 1 };
    private static UpdateTicketRequest ValidUpdate() => new() { Title = "Updated", Description = "Description", CategoryId = 1, PriorityId = 1 };

    private static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(code, json.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("traceId").GetString()));
    }
}
