using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Contracts.Dashboard;
using HelpDesk.Api.Data;
using HelpDesk.Api.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HelpDesk.Api.Tests;

public sealed class DashboardDataIsolationEndpointIntegrationTests : IAsyncLifetime
{
    private readonly RealDashboardApiFactory factory = new();

    public Task InitializeAsync() => factory.InitializeDatabaseAsync();
    public async Task DisposeAsync() => await factory.DisposeAsync();

    [Fact]
    public async Task EmployeeA_AllDashboardSectionsContainOnlyCreatedTickets()
    {
        var (dashboard, json) = await GetDashboardAsync(RealDashboardApiFactory.EmployeeAId, AppRoles.Employee);

        Assert.Equal(1, dashboard.Summary.TotalTickets);
        Assert.Equal(1, dashboard.Summary.OpenTickets);
        Assert.Equal(0, dashboard.Summary.InProgressTickets);
        Assert.Equal(0, dashboard.Summary.PendingTickets);
        Assert.Equal(0, dashboard.Summary.ClosedTickets);
        Assert.Equal(0, dashboard.Summary.AssignedTickets);
        Assert.Equal(1, dashboard.Summary.UnassignedTickets);
        Assert.Equal(1, dashboard.Summary.CriticalTickets);
        Assert.Equal(1, dashboard.Summary.CreatedThisMonth);
        Assert.Equal(0, dashboard.Summary.ClosedThisMonth);
        AssertBreakdown(dashboard.StatusBreakdown, ("Open", 1), ("In Progress", 0), ("Closed", 0));
        AssertBreakdown(dashboard.PriorityBreakdown, ("Low", 0), ("Medium", 0), ("Critical", 1));
        AssertBreakdown(dashboard.CategoryBreakdown, ("Hardware", 0), ("Software", 0), ("Network", 1), ("Other", 0));
        Assert.Equal(1, dashboard.MonthlyTrend.Sum(x => x.CreatedCount));
        Assert.Equal(0, dashboard.MonthlyTrend.Sum(x => x.ClosedCount));
        Assert.Equal(0, dashboard.MonthlyTrend.Sum(x => x.CancelledCount));
        var recent = Assert.Single(dashboard.RecentTickets);
        Assert.Equal("TKT-A", recent.ReferenceNumber);
        Assert.Null(recent.AssignedToDisplayName);
        Assert.DoesNotContain("TKT-B", json, StringComparison.Ordinal);
        Assert.DoesNotContain("TKT-C", json, StringComparison.Ordinal);
        AssertSafeSerializedResponse(json);
    }

    [Fact]
    public async Task EmployeeB_SeesOnlyBothEmployeeBCreatedTickets()
    {
        var (dashboard, _) = await GetDashboardAsync(RealDashboardApiFactory.EmployeeBId, AppRoles.Employee);

        Assert.Equal(2, dashboard.Summary.TotalTickets);
        Assert.Equal(1, dashboard.Summary.InProgressTickets);
        Assert.Equal(1, dashboard.Summary.ClosedTickets);
        Assert.Equal(2, dashboard.Summary.AssignedTickets);
        Assert.Equal(0, dashboard.Summary.UnassignedTickets);
        Assert.Equal(0, dashboard.Summary.CriticalTickets);
        Assert.Equal(1, dashboard.Summary.CreatedThisMonth);
        Assert.Equal(1, dashboard.Summary.ClosedThisMonth);
        AssertBreakdown(dashboard.PriorityBreakdown, ("Low", 1), ("Medium", 1), ("Critical", 0));
        AssertBreakdown(dashboard.CategoryBreakdown, ("Hardware", 1), ("Software", 1), ("Network", 0), ("Other", 0));
        Assert.Equal(2, dashboard.MonthlyTrend.Sum(x => x.CreatedCount));
        Assert.Equal(1, dashboard.MonthlyTrend.Sum(x => x.ClosedCount));
        Assert.Equal(["TKT-C", "TKT-B"], dashboard.RecentTickets.Select(x => x.ReferenceNumber));
        Assert.Equal(["Employee A", "Support Agent"], dashboard.RecentTickets.Select(x => x.AssignedToDisplayName));
    }

    [Fact]
    public async Task Manager_SeesOnlyManagerCreatedTicket()
    {
        var (dashboard, _) = await GetDashboardAsync(RealDashboardApiFactory.ManagerId, AppRoles.Manager);

        Assert.Equal(1, dashboard.Summary.TotalTickets);
        Assert.Equal(1, dashboard.Summary.PendingTickets);
        Assert.Equal(1, dashboard.Summary.AssignedTickets);
        Assert.Equal(1, dashboard.Summary.CancelledTickets);
        Assert.Equal(0, dashboard.Summary.CreatedThisMonth);
        AssertBreakdown(dashboard.PriorityBreakdown, ("Medium", 1), ("Critical", 0));
        Assert.Equal(1, dashboard.MonthlyTrend.Sum(x => x.CreatedCount));
        Assert.Equal(1, dashboard.MonthlyTrend.Sum(x => x.CancelledCount));
        var recent = Assert.Single(dashboard.RecentTickets);
        Assert.Equal("TKT-D", recent.ReferenceNumber);
        Assert.Equal("Administrator", recent.AssignedToDisplayName);
    }

    [Theory]
    [InlineData(AppRoles.ItSupportAgent)]
    [InlineData(AppRoles.Admin)]
    public async Task SupportRoles_SeeOrganizationWideDashboard(string role)
    {
        var userId = role == AppRoles.Admin ? RealDashboardApiFactory.AdminId : RealDashboardApiFactory.AgentId;
        var (dashboard, json) = await GetDashboardAsync(userId, role);

        AssertOrganizationWide(dashboard);
        AssertSafeSerializedResponse(json);
    }

    [Fact]
    public async Task EmployeePlusSupportAgent_SupportScopeWins()
    {
        var (dashboard, _) = await GetDashboardAsync(
            RealDashboardApiFactory.EmployeeAId, AppRoles.Employee, AppRoles.ItSupportAgent);

        AssertOrganizationWide(dashboard);
    }

    [Fact]
    public async Task EmployeePlusManager_RemainsCreatorOwned()
    {
        var (dashboard, _) = await GetDashboardAsync(
            RealDashboardApiFactory.EmployeeAId, AppRoles.Employee, AppRoles.Manager);

        Assert.Equal(1, dashboard.Summary.TotalTickets);
        Assert.Equal("TKT-A", Assert.Single(dashboard.RecentTickets).ReferenceNumber);
        Assert.Equal(1, dashboard.StatusBreakdown.Sum(x => x.Count));
        Assert.Equal(1, dashboard.PriorityBreakdown.Sum(x => x.Count));
        Assert.Equal(1, dashboard.CategoryBreakdown.Sum(x => x.Count));
    }

    [Fact]
    public async Task HeadersAndQueryParameters_CannotOverrideJwtIdentityOrRole()
    {
        var client = factory.CreateAuthorizedClient(RealDashboardApiFactory.EmployeeAId, AppRoles.Employee);
        client.DefaultRequestHeaders.Add("X-User-Id", RealDashboardApiFactory.EmployeeBId.ToString());
        client.DefaultRequestHeaders.Add("X-Role", AppRoles.Admin);
        client.DefaultRequestHeaders.Add("X-Roles", AppRoles.Admin);
        var response = await client.GetAsync(
            $"/api/dashboard?userId={RealDashboardApiFactory.EmployeeBId}&role={Uri.EscapeDataString(AppRoles.Admin)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dashboard = await response.Content.ReadFromJsonAsync<DashboardResponse>();
        Assert.NotNull(dashboard);
        Assert.Equal(1, dashboard.Summary.TotalTickets);
        Assert.Equal("TKT-A", Assert.Single(dashboard.RecentTickets).ReferenceNumber);
    }

    private async Task<(DashboardResponse Dashboard, string Json)> GetDashboardAsync(Guid userId, params string[] roles)
    {
        var response = await factory.CreateAuthorizedClient(userId, roles).GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        return (System.Text.Json.JsonSerializer.Deserialize<DashboardResponse>(json,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))!, json);
    }

    private static void AssertOrganizationWide(DashboardResponse dashboard)
    {
        Assert.Equal(4, dashboard.Summary.TotalTickets);
        Assert.Equal(1, dashboard.Summary.OpenTickets);
        Assert.Equal(1, dashboard.Summary.InProgressTickets);
        Assert.Equal(1, dashboard.Summary.PendingTickets);
        Assert.Equal(1, dashboard.Summary.ClosedTickets);
        Assert.Equal(3, dashboard.Summary.AssignedTickets);
        Assert.Equal(1, dashboard.Summary.UnassignedTickets);
        Assert.Equal(1, dashboard.Summary.CriticalTickets);
        Assert.Equal(2, dashboard.Summary.CreatedThisMonth);
        Assert.Equal(1, dashboard.Summary.ClosedThisMonth);
        Assert.Equal(1, dashboard.Summary.CancelledTickets);
        AssertBreakdown(dashboard.StatusBreakdown, ("Open", 1), ("In Progress", 1), ("Pending", 1), ("Resolved", 0), ("Closed", 1));
        AssertBreakdown(dashboard.PriorityBreakdown, ("Low", 1), ("Medium", 2), ("Critical", 1));
        AssertBreakdown(dashboard.CategoryBreakdown, ("Hardware", 2), ("Software", 1), ("Network", 1), ("Other", 0));
        Assert.Equal(4, dashboard.MonthlyTrend.Sum(x => x.CreatedCount));
        Assert.Equal(1, dashboard.MonthlyTrend.Sum(x => x.ClosedCount));
        Assert.Equal(1, dashboard.MonthlyTrend.Sum(x => x.CancelledCount));
        Assert.Equal(["TKT-A", "TKT-C", "TKT-B", "TKT-D"], dashboard.RecentTickets.Select(x => x.ReferenceNumber));
    }

    private static void AssertBreakdown(
        IReadOnlyList<DashboardBreakdownItemResponse> actual,
        params (string Name, int Count)[] expected)
    {
        foreach (var (name, count) in expected)
            Assert.Equal(count, actual.Single(x => x.Name == name).Count);
    }

    private static void AssertSafeSerializedResponse(string json)
    {
        Assert.DoesNotContain("email", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("credential", json, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class RealDashboardApiFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        private readonly ServiceProvider sqliteServices = new ServiceCollection().AddEntityFrameworkSqlite().BuildServiceProvider();
        public static Guid EmployeeAId { get; } = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public static Guid EmployeeBId { get; } = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        public static Guid ManagerId { get; } = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        public static Guid AgentId { get; } = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        public static Guid AdminId { get; } = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        private static DateTime Now { get; } = new(2026, 8, 18, 12, 0, 0, DateTimeKind.Utc);

        public async Task InitializeDatabaseAsync()
        {
            await connection.OpenAsync();
            await TicketSqliteDatabase.InitializeAsync(connection);
            _ = Services;
            await using var scope = Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Users.AddRange(
                User(EmployeeAId, "Employee A"), User(EmployeeBId, "Employee B"), User(ManagerId, "Manager"),
                User(AgentId, "Support Agent"), User(AdminId, "Administrator"));
            db.Priorities.Add(new Priority { Id=3, Name="Critical", Rank=3, IsActive=true, CreatedAtUtc=Now, UpdatedAtUtc=Now });
            db.Categories.AddRange(
                new Category { Id=3, Name="Network", SortOrder=3, IsActive=true, CreatedAtUtc=Now, UpdatedAtUtc=Now },
                new Category { Id=4, Name="Other", SortOrder=4, IsActive=true, CreatedAtUtc=Now, UpdatedAtUtc=Now });
            db.Tickets.AddRange(
                Ticket("10000000-0000-0000-0000-000000000001", "TKT-A", EmployeeAId, null, 1, 3, 3,
                    new DateTime(2026,8,4,0,0,0,DateTimeKind.Utc), Now),
                Ticket("20000000-0000-0000-0000-000000000002", "TKT-B", EmployeeBId, AgentId, 5, 2, 1,
                    new DateTime(2026,7,10,0,0,0,DateTimeKind.Utc), Now.AddMinutes(-2),
                    closed: new DateTime(2026,8,2,0,0,0,DateTimeKind.Utc)),
                Ticket("30000000-0000-0000-0000-000000000003", "TKT-C", EmployeeBId, EmployeeAId, 2, 1, 2,
                    new DateTime(2026,8,3,0,0,0,DateTimeKind.Utc), Now.AddMinutes(-1)),
                Ticket("40000000-0000-0000-0000-000000000004", "TKT-D", ManagerId, AdminId, 3, 2, 1,
                    new DateTime(2026,6,15,0,0,0,DateTimeKind.Utc), Now.AddMinutes(-3),
                    cancelled: new DateTime(2026,7,20,0,0,0,DateTimeKind.Utc)));
            await db.SaveChangesAsync();
        }

        public HttpClient CreateAuthorizedClient(Guid userId, params string[] roles)
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthApiFactory.CreateJwt(userId, roles));
            return client;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string,string?>
                {
                    ["ConnectionStrings:DefaultConnection"]="Host=127.0.0.1;Port=1;Database=not_used;Username=test;Password=test",
                    ["Frontend:AllowedOrigins:0"]="https://frontend.test", ["Jwt:Issuer"]=AuthApiFactory.TestIssuer,
                    ["Jwt:Audience"]=AuthApiFactory.TestAudience, ["Jwt:SecretKey"]=AuthApiFactory.TestSecret,
                    ["Jwt:AccessTokenLifetimeMinutes"]="5", ["Jwt:RefreshTokenLifetimeDays"]="1"
                }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ApplicationDbContext>();
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection).UseInternalServiceProvider(sqliteServices));
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(new FixedTimeProvider(new DateTimeOffset(Now)));
            });
        }

        private static User User(Guid id,string name) => new() { Id=id,UserName=$"{id}@test",NormalizedUserName=$"{id}@TEST",
            Email=$"{id}@test",NormalizedEmail=$"{id}@TEST",DisplayName=name,IsActive=true,SecurityStamp=Guid.NewGuid().ToString(),
            ConcurrencyStamp=Guid.NewGuid().ToString(),CreatedAtUtc=Now,UpdatedAtUtc=Now };
        private static Ticket Ticket(string id,string number,Guid creator,Guid? assignee,short status,short priority,short category,
            DateTime created,DateTime updated,DateTime? closed=null,DateTime? cancelled=null) => new() { Id=Guid.Parse(id),ReferenceNumber=number,
            Title=$"{number} title",Description="Safe test description",CreatedByUserId=creator,AssignedToUserId=assignee,StatusId=status,
            PriorityId=priority,CategoryId=category,CreatedAtUtc=created,UpdatedAtUtc=updated,ClosedAtUtc=closed,CancelledAtUtc=cancelled };
        public override async ValueTask DisposeAsync() { await base.DisposeAsync();await connection.DisposeAsync();await sqliteServices.DisposeAsync(); }
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
