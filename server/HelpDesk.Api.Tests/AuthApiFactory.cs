using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using HelpDesk.Api.Application.Auth;
using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Contracts.Auth;
using HelpDesk.Api.Contracts.Common;
using HelpDesk.Api.Contracts.Tickets;
using HelpDesk.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace HelpDesk.Api.Tests;

public sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    public const string TestIssuer = "HelpDesk.Api.Tests";
    public const string TestAudience = "HelpDesk.Api.Tests.Client";
    public const string TestSecret = "TEST_ONLY_JWT_SIGNING_KEY_32_BYTES_MINIMUM_2026";

    public AuthApiFactory()
    {
        AuthenticationService = new Mock<IAuthenticationService>();
        AuthenticationService.Setup(service => service.RegisterAsync(
                It.IsAny<RegisterRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthResponse());
        AuthenticationService.Setup(service => service.LoginAsync(
                It.IsAny<LoginRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthResponse());
        AuthenticationService.Setup(service => service.RefreshAsync(
                It.IsAny<RefreshTokenRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthResponse());
        AuthenticationService.Setup(service => service.LogoutAsync(
                It.IsAny<LogoutRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        AuthenticationService.Setup(service => service.GetCurrentUserAsync(
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid userId, CancellationToken _) => CurrentUserResponse(userId));
        TicketService = new Mock<ITicketService>();
        TicketLookupService = new Mock<ITicketLookupService>();
        TicketService.Setup(x => x.CreateAsync(It.IsAny<CreateTicketRequest>(), It.IsAny<TicketAccessContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TicketDetail());
        TicketService.Setup(x => x.GetPagedAsync(It.IsAny<TicketListRequest>(), It.IsAny<TicketAccessContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<TicketSummaryResponse> { PageNumber = 1, PageSize = 20 });
        TicketService.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<TicketAccessContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TicketDetail());
        TicketService.Setup(x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateTicketRequest>(), It.IsAny<TicketAccessContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TicketDetail());
        TicketService.Setup(x => x.AssignAsync(It.IsAny<Guid>(), It.IsAny<AssignTicketRequest>(), It.IsAny<TicketAccessContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TicketDetail());
        TicketService.Setup(x => x.ChangeStatusAsync(It.IsAny<Guid>(), It.IsAny<ChangeTicketStatusRequest>(), It.IsAny<TicketAccessContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TicketDetail());
        TicketService.Setup(x => x.AddCommentAsync(It.IsAny<Guid>(), It.IsAny<AddTicketCommentRequest>(), It.IsAny<TicketAccessContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TicketCommentResponse { Id = CommentId, TicketId = TicketId, Body = "ok", Visibility = "Public" });
        TicketLookupService.Setup(x => x.GetCategoriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<TicketCategoryResponse>());
        TicketLookupService.Setup(x => x.GetPrioritiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<TicketPriorityResponse>());
        TicketLookupService.Setup(x => x.GetStatusesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<TicketStatusResponse>());
    }

    public Mock<IAuthenticationService> AuthenticationService { get; }
    public Mock<ITicketService> TicketService { get; }
    public Mock<ITicketLookupService> TicketLookupService { get; }
    public static Guid TicketId { get; } = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    public static Guid CommentId { get; } = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    public HttpClient CreateAuthorizedClient(Guid userId, params string[] roles)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateJwt(userId, roles));
        return client;
    }

    public static string CreateJwt(Guid userId, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=127.0.0.1;Port=1;Database=not_used;Username=test;Password=test",
                ["Jwt:Issuer"] = TestIssuer,
                ["Jwt:Audience"] = TestAudience,
                ["Jwt:SecretKey"] = TestSecret,
                ["Jwt:AccessTokenLifetimeMinutes"] = "5",
                ["Jwt:RefreshTokenLifetimeDays"] = "1"
            });
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAuthenticationService>();
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddSingleton(AuthenticationService.Object);
            services.RemoveAll<ITicketService>();
            services.RemoveAll<ITicketLookupService>();
            services.AddSingleton(TicketService.Object);
            services.AddSingleton(TicketLookupService.Object);
        });
    }

    private static AuthResponse AuthResponse() => new()
    {
        AccessToken = "test-access-token",
        ExpiresAtUtc = new DateTime(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc),
        RefreshToken = "test-refresh-token",
        RefreshTokenExpiresAtUtc = new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc),
        UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        Email = "employee@example.test",
        DisplayName = "Test Employee",
        Roles = ["Employee"]
    };

    private static TicketDetailResponse TicketDetail() => new()
    {
        Id = TicketId, TicketNumber = "TKT-TEST", Title = "Test", Description = "Test",
        CategoryId = 1, CategoryName = "Hardware", PriorityId = 1, PriorityName = "Low",
        StatusId = 1, StatusName = "Open", CreatedByUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        CreatedByDisplayName = "Test User"
    };

    private static CurrentUserResponse CurrentUserResponse(Guid userId) => new()
    {
        UserId = userId,
        Email = "employee@example.test",
        DisplayName = "Test Employee",
        Roles = ["Employee"],
        IsActive = true
    };
}
