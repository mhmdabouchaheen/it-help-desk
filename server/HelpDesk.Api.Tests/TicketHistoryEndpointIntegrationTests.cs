using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Tickets;
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

public sealed class TicketHistoryEndpointIntegrationTests : IAsyncLifetime
{
    private readonly RealTicketApiFactory factory = new();

    public Task InitializeAsync() => factory.InitializeDatabaseAsync();
    public async Task DisposeAsync() => await factory.DisposeAsync();

    [Theory]
    [InlineData(AppRoles.Employee)]
    [InlineData(AppRoles.Manager)]
    public async Task NonSupportOwner_ResponseOmitsInternalCommentBody(string role)
    {
        var response = await factory.CreateAuthorizedClient(RealTicketApiFactory.OwnerId, role)
            .GetAsync($"/api/tickets/{RealTicketApiFactory.TicketId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var comment = Assert.Single(json.GetProperty("comments").EnumerateArray());
        Assert.Equal("Public endpoint update", comment.GetProperty("body").GetString());
        Assert.Equal(TicketCommentVisibilities.Public, comment.GetProperty("visibility").GetString());
        Assert.DoesNotContain("Internal endpoint diagnosis", json.GetRawText(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.ItSupportAgent)]
    public async Task Support_ResponseIncludesPublicAndInternalCommentsInOrder(string role)
    {
        var response = await factory.CreateAuthorizedClient(RealTicketApiFactory.SupportId, role)
            .GetAsync($"/api/tickets/{RealTicketApiFactory.TicketId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var comments = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement
            .GetProperty("comments").EnumerateArray().ToArray();
        Assert.Equal(["Public endpoint update", "Internal endpoint diagnosis"],
            comments.Select(x => x.GetProperty("body").GetString()));
        Assert.Equal([TicketCommentVisibilities.Public, TicketCommentVisibilities.Internal],
            comments.Select(x => x.GetProperty("visibility").GetString()));
    }

    [Fact]
    public async Task NonOwnerEmployee_RetainsNotFoundBehavior()
    {
        var response = await factory.CreateAuthorizedClient(RealTicketApiFactory.OtherId, AppRoles.Employee)
            .GetAsync($"/api/tickets/{RealTicketApiFactory.TicketId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("Internal endpoint diagnosis", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    private sealed class RealTicketApiFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        private readonly ServiceProvider sqliteServices = new ServiceCollection()
            .AddEntityFrameworkSqlite()
            .BuildServiceProvider();
        public static Guid OwnerId { get; } = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        public static Guid SupportId { get; } = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        public static Guid OtherId { get; } = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        public static Guid TicketId { get; } = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        public async Task InitializeDatabaseAsync()
        {
            await connection.OpenAsync();
            await TicketSqliteDatabase.InitializeAsync(connection);
            _ = Services;
            await using var scope = Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var now = new DateTime(2026, 8, 18, 12, 0, 0, DateTimeKind.Utc);
            db.Users.AddRange(User(OwnerId, "Ticket Owner"), User(SupportId, "Support User"), User(OtherId, "Other Employee"));
            db.Tickets.Add(new Ticket
            {
                Id = TicketId, ReferenceNumber = "TKT-HISTORY", Title = "History security",
                Description = "Endpoint regression ticket", CategoryId = 1, PriorityId = 1, StatusId = 1,
                CreatedByUserId = OwnerId, CreatedAtUtc = now.AddMinutes(-3), UpdatedAtUtc = now
            });
            db.TicketComments.AddRange(
                new TicketComment
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), TicketId = TicketId,
                    AuthorUserId = OwnerId, Body = "Public endpoint update",
                    Visibility = TicketCommentVisibilities.Public, CreatedAtUtc = now.AddMinutes(-2)
                },
                new TicketComment
                {
                    Id = Guid.Parse("20000000-0000-0000-0000-000000000002"), TicketId = TicketId,
                    AuthorUserId = SupportId, Body = "Internal endpoint diagnosis",
                    Visibility = TicketCommentVisibilities.Internal, CreatedAtUtc = now.AddMinutes(-1)
                });
            await db.SaveChangesAsync();
        }

        public HttpClient CreateAuthorizedClient(Guid userId, params string[] roles)
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", AuthApiFactory.CreateJwt(userId, roles));
            return client;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=127.0.0.1;Port=1;Database=not_used;Username=test;Password=test",
                    ["Frontend:AllowedOrigins:0"] = "https://frontend.test",
                    ["Jwt:Issuer"] = AuthApiFactory.TestIssuer,
                    ["Jwt:Audience"] = AuthApiFactory.TestAudience,
                    ["Jwt:SecretKey"] = AuthApiFactory.TestSecret,
                    ["Jwt:AccessTokenLifetimeMinutes"] = "5",
                    ["Jwt:RefreshTokenLifetimeDays"] = "1"
                }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ApplicationDbContext>();
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options => options
                    .UseSqlite(connection)
                    .UseInternalServiceProvider(sqliteServices));
            });
        }

        private static User User(Guid id, string name) => new()
        {
            Id = id, UserName = $"{id}@test", NormalizedUserName = $"{id}@TEST",
            Email = $"{id}@test", NormalizedEmail = $"{id}@TEST", DisplayName = name, IsActive = true,
            SecurityStamp = Guid.NewGuid().ToString(), ConcurrencyStamp = Guid.NewGuid().ToString(),
            CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow
        };

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            await connection.DisposeAsync();
            await sqliteServices.DisposeAsync();
        }
    }
}
