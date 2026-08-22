using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using HelpDesk.Api.Application.Auth;
using HelpDesk.Api.Application.Audit;
using HelpDesk.Api.Application.Attachments;
using HelpDesk.Api.Application.Dashboard;
using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Application.Notifications;
using HelpDesk.Api.Application.Reports;
using HelpDesk.Api.Application.Ai;
using HelpDesk.Api.Application.Users;
using HelpDesk.Api.Contracts.Auth;
using HelpDesk.Api.Contracts.Audit;
using HelpDesk.Api.Contracts.Common;
using HelpDesk.Api.Contracts.Dashboard;
using HelpDesk.Api.Contracts.Tickets;
using HelpDesk.Api.Contracts.Notifications;
using HelpDesk.Api.Contracts.Reports;
using HelpDesk.Api.Contracts.Ai;
using HelpDesk.Api.Contracts.Users;
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
        TicketAttachmentService = new Mock<ITicketAttachmentService>();
        DashboardService = new Mock<IDashboardService>();
        ReportService = new Mock<IReportService>();
        ReportExportService = new Mock<IReportExportService>();
        AiTicketAnalysisService = new Mock<IAiTicketAnalysisService>();
        NotificationService = new Mock<INotificationService>();
        ActivityLogService = new Mock<IActivityLogService>();
        ActivityLogService.Setup(x=>x.GetPagedAsync(It.IsAny<ActivityLogListRequest>(),It.IsAny<CancellationToken>())).ReturnsAsync(new PagedResponse<ActivityLogResponse>{PageNumber=1,PageSize=20});
        ActivityLogService.Setup(x=>x.GetForTicketAsync(It.IsAny<Guid>(),It.IsAny<PagedRequest>(),It.IsAny<CancellationToken>())).ReturnsAsync(new PagedResponse<ActivityLogResponse>{PageNumber=1,PageSize=20});
        NotificationService.Setup(x=>x.GetPagedAsync(It.IsAny<Guid>(),It.IsAny<NotificationListRequest>(),It.IsAny<CancellationToken>())).ReturnsAsync(new PagedResponse<NotificationResponse>{PageNumber=1,PageSize=20});
        NotificationService.Setup(x=>x.GetUnreadCountAsync(It.IsAny<Guid>(),It.IsAny<CancellationToken>())).ReturnsAsync(new NotificationUnreadCountResponse());
        DashboardService.Setup(x => x.GetDashboardAsync(It.IsAny<TicketAccessContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardResponse());
        ReportService.Setup(x => x.GetTicketReportAsync(It.IsAny<TicketReportRequest>(), It.IsAny<TicketAccessContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(new TicketReportResponse());
        ReportExportService.Setup(x=>x.ExportTicketReportPdfAsync(It.IsAny<TicketReportRequest>(),It.IsAny<TicketAccessContext>(),It.IsAny<CancellationToken>())).ReturnsAsync(new ReportExportResult([1,2,3],"application/pdf","ticket-report-20260817-120000.pdf"));
        ReportExportService.Setup(x=>x.ExportTicketReportExcelAsync(It.IsAny<TicketReportRequest>(),It.IsAny<TicketAccessContext>(),It.IsAny<CancellationToken>())).ReturnsAsync(new ReportExportResult([1,2,3],"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet","ticket-report-20260817-120000.xlsx"));
        AiTicketAnalysisService.Setup(x=>x.AnalyzeTicketAsync(It.IsAny<Guid>(),It.IsAny<TicketAccessContext>(),It.IsAny<CancellationToken>())).ReturnsAsync(new AiTicketAnalysisResponse{Summary="Safe summary"});
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
        AuthenticationService.Setup(service => service.ForgotPasswordAsync(
                It.IsAny<ForgotPasswordRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        AuthenticationService.Setup(service => service.ResetPasswordAsync(
                It.IsAny<ResetPasswordRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        AuthenticationService.Setup(service => service.UpdateProfileAsync(
                It.IsAny<Guid>(), It.IsAny<UpdateProfileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid userId, UpdateProfileRequest request, CancellationToken _) => new CurrentUserResponse
            { UserId = userId, Email = "employee@example.test", DisplayName = request.DisplayName, Roles = ["Employee"], IsActive = true });
        AuthenticationService.Setup(service => service.ChangePasswordAsync(
                It.IsAny<Guid>(), It.IsAny<ChangePasswordRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        TicketService = new Mock<ITicketService>();
        TicketLookupService = new Mock<ITicketLookupService>();
        SupportUserDirectoryService = new Mock<ISupportUserDirectoryService>();
        UserTeamManagementService = new Mock<IUserTeamManagementService>();
        UserRoleManagementService = new Mock<IUserRoleManagementService>();
        SupportUserDirectoryService.Setup(x => x.GetEligibleSupportUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new SupportUserResponse { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), DisplayName = "Support User", Roles = ["IT Support Agent"] }]);
        UserTeamManagementService.Setup(x=>x.GetUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        UserTeamManagementService.Setup(x=>x.UpdateManagerAsync(It.IsAny<Guid>(),It.IsAny<UpdateUserManagerRequest>(),It.IsAny<CancellationToken>())).ReturnsAsync(new TeamMemberResponse());
        UserRoleManagementService.Setup(x=>x.GetUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        UserRoleManagementService.Setup(x=>x.UpdateRolesAsync(It.IsAny<Guid>(),It.IsAny<UpdateUserRolesRequest>(),It.IsAny<Guid>(),It.IsAny<string?>(),It.IsAny<CancellationToken>())).ReturnsAsync(new RoleManagedUserResponse());
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
        TicketService.Setup(x => x.CancelAsync(It.IsAny<Guid>(), It.IsAny<CancelTicketRequest>(), It.IsAny<TicketAccessContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TicketDetail());
        TicketLookupService.Setup(x => x.GetCategoriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<TicketCategoryResponse>());
        TicketLookupService.Setup(x => x.GetPrioritiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<TicketPriorityResponse>());
        TicketLookupService.Setup(x => x.GetStatusesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<TicketStatusResponse>());
        TicketAttachmentService.Setup(x => x.UploadAsync(It.IsAny<Guid>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<TicketAccessContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TicketAttachmentResponse { Id = Guid.Parse("abababab-abab-abab-abab-abababababab"), TicketId = TicketId, OriginalFileName = "safe.txt", ContentType = "text/plain", SizeBytes = 4, UploadedByUserId = Guid.NewGuid(), UploadedByDisplayName = "Uploader" });
        TicketAttachmentService.Setup(x => x.DownloadAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<TicketAccessContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AttachmentDownloadResult(new MemoryStream("safe"u8.ToArray()), "text/plain", "safe.txt", 4));
    }

    public Mock<IAuthenticationService> AuthenticationService { get; }
    public Mock<ITicketService> TicketService { get; }
    public Mock<ITicketLookupService> TicketLookupService { get; }
    public Mock<ITicketAttachmentService> TicketAttachmentService { get; }
    public Mock<IDashboardService> DashboardService { get; }
    public Mock<IReportService> ReportService { get; }
    public Mock<IReportExportService> ReportExportService { get; }
    public Mock<IAiTicketAnalysisService> AiTicketAnalysisService { get; }
    public Mock<INotificationService> NotificationService { get; }
    public Mock<IActivityLogService> ActivityLogService { get; }
    public Mock<ISupportUserDirectoryService> SupportUserDirectoryService { get; }
    public Mock<IUserTeamManagementService> UserTeamManagementService { get; }
    public Mock<IUserRoleManagementService> UserRoleManagementService { get; }
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
                ["Frontend:AllowedOrigins:0"] = "https://frontend.test",
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
            services.RemoveAll<ITicketAttachmentService>();
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddSingleton(AuthenticationService.Object);
            services.AddSingleton(TicketAttachmentService.Object);
            services.RemoveAll<ITicketService>();
            services.RemoveAll<IDashboardService>();
            services.RemoveAll<IReportService>();
            services.RemoveAll<IReportExportService>();
            services.RemoveAll<IAiTicketAnalysisService>();
            services.RemoveAll<INotificationService>();
            services.RemoveAll<IActivityLogService>();
            services.RemoveAll<ITicketLookupService>();
            services.RemoveAll<ISupportUserDirectoryService>();
            services.RemoveAll<IUserTeamManagementService>();
            services.RemoveAll<IUserRoleManagementService>();
            services.AddSingleton(TicketService.Object);
            services.AddSingleton(DashboardService.Object);
            services.AddSingleton(ReportService.Object);
            services.AddSingleton(ReportExportService.Object);
            services.AddSingleton(AiTicketAnalysisService.Object);
            services.AddSingleton(NotificationService.Object);
            services.AddSingleton(ActivityLogService.Object);
            services.AddSingleton(TicketLookupService.Object);
            services.AddSingleton(SupportUserDirectoryService.Object);
            services.AddSingleton(UserTeamManagementService.Object);
            services.AddSingleton(UserRoleManagementService.Object);
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
