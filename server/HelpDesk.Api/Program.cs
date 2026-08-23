using System.Security.Claims;
using System.Text;
using System.Text.Json;
using HelpDesk.Api.Application.Auth;
using HelpDesk.Api.Application.Dashboard;
using HelpDesk.Api.Application.Attachments;
using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Application.Users;
using HelpDesk.Api.Application.Notifications;
using HelpDesk.Api.Application.Reports;
using HelpDesk.Api.Application.Ai;
using HelpDesk.Api.Configuration;
using HelpDesk.Api.Data;
using HelpDesk.Api.Entities;
using HelpDesk.Api.Infrastructure.Auth;
using HelpDesk.Api.Infrastructure.Dashboard;
using HelpDesk.Api.Infrastructure.Attachments;
using HelpDesk.Api.Infrastructure.Authorization;
using HelpDesk.Api.Infrastructure.ExceptionHandling;
using HelpDesk.Api.Infrastructure.Tickets;
using HelpDesk.Api.Infrastructure.Users;
using HelpDesk.Api.Infrastructure.Notifications;
using HelpDesk.Api.Infrastructure.Reports;
using HelpDesk.Api.Infrastructure.Ai;
using HelpDesk.Api.Infrastructure.Identity;
using HelpDesk.Api.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
ReportPdfFontConfiguration.Configure();

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
    LoadDevelopmentDotEnv(builder.Environment.ContentRootPath);
    builder.Configuration.AddEnvironmentVariables();
}

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??=
            new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT bearer authentication using the Authorization header."
        };
        return Task.CompletedTask;
    });
});
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddSignalR();
builder.Services.AddOptions<AttachmentOptions>()
    .Bind(builder.Configuration.GetSection(AttachmentOptions.SectionName))
    .Validate(x => !string.IsNullOrWhiteSpace(x.StorageRoot), "Attachment storage root must not be blank.")
    .Validate(x => x.MaxFileSizeBytes > 0, "Attachment maximum size must be positive.")
    .Validate(x => x.AllowedContentTypes.Length > 0 && x.AllowedContentTypes.All(v => !string.IsNullOrWhiteSpace(v) && !v.Contains('*')), "Attachment content types must be explicit.")
    .Validate(x => x.AllowedExtensions.Length > 0, "Attachment extensions must not be empty.")
    .Validate(x => x.AllowedExtensions.All(v => new[] { ".png", ".jpg", ".jpeg", ".webp", ".pdf", ".txt", ".docx", ".xlsx" }.Contains(v.StartsWith('.') ? v.ToLowerInvariant() : "." + v.ToLowerInvariant())), "Attachment extensions contain an unsafe or unsupported value.")
    .ValidateOnStart();
var frontendOrigins = builder.Configuration.GetSection("Frontend:AllowedOrigins").Get<string[]>() ?? [];
if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing") && frontendOrigins.Length == 0)
    throw new InvalidOperationException("At least one production frontend origin must be configured.");
if (frontendOrigins.Any(origin => origin.Contains('*') || !Uri.TryCreate(origin,UriKind.Absolute,out var uri) ||
    uri.Scheme is not ("http" or "https")))
    throw new InvalidOperationException("Frontend origins must be explicit absolute HTTP or HTTPS origins without wildcards.");
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(frontendOrigins).AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problem = new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Detail = "One or more request fields are invalid.",
                Instance = context.HttpContext.Request.Path
            };
            problem.Extensions["code"] = "validation_failed";
            problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

            return new BadRequestObjectResult(problem)
            {
                ContentTypes = { "application/problem+json" }
            };
        };
    });
builder.Services.AddDataProtection();
builder.Services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromHours(1));
builder.Services.Configure<PasswordResetEmailOptions>(builder.Configuration.GetSection(PasswordResetEmailOptions.SectionName));
builder.Services.AddScoped<IPasswordResetEmailSender, SmtpPasswordResetEmailSender>();
builder.Services.AddRateLimiter(options => options.AddPolicy("PasswordReset", context =>
    RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        })));
builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer),
        "JWT issuer must not be blank.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Audience),
        "JWT audience must not be blank.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.SecretKey),
        "JWT secret key must not be blank.")
    .Validate(options => Encoding.UTF8.GetByteCount(options.SecretKey) >= 32,
        "JWT secret key must be at least 32 UTF-8 bytes.")
    .Validate(options => options.AccessTokenLifetimeMinutes > 0,
        "JWT access-token lifetime must be greater than zero.")
    .Validate(options => options.RefreshTokenLifetimeDays > 0,
        "JWT refresh-token lifetime must be greater than zero.")
    .ValidateOnStart();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);
builder.Services
    .AddIdentityCore<User>()
    .AddRoles<Role>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer();
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt.SecretKey)),
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.HttpContext.Request.Path.Equals("/hubs/notifications", StringComparison.Ordinal) &&
                    context.Request.Query.TryGetValue("access_token", out var token) && token.Count == 1)
                    context.Token = token[0];
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                context.HandleResponse();
                return WriteAuthenticationProblemAsync(
                    context.HttpContext,
                    StatusCodes.Status401Unauthorized,
                    "Authentication required",
                    "A valid bearer access token is required.",
                    "authentication_required");
            },
            OnForbidden = context => WriteAuthenticationProblemAsync(
                context.HttpContext,
                StatusCodes.Status403Forbidden,
                "Access forbidden",
                "The authenticated user is not authorized for this operation.",
                "access_forbidden")
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppPolicies.AuthenticatedUser, policy =>
        policy.RequireAuthenticatedUser());
    options.AddPolicy(AppPolicies.AdminOnly, policy =>
        policy.RequireAuthenticatedUser().RequireRole(AppRoles.Admin));
    options.AddPolicy(AppPolicies.SupportStaff, policy =>
        policy.RequireAuthenticatedUser().RequireRole(AppRoles.Admin, AppRoles.ItSupportAgent));
    options.AddPolicy(AppPolicies.Management, policy =>
        policy.RequireAuthenticatedUser().RequireRole(AppRoles.Admin, AppRoles.Manager));
    options.AddPolicy(AppPolicies.ManagementOrSupport, policy =>
        policy.RequireAuthenticatedUser().RequireRole(AppRoles.Admin, AppRoles.ItSupportAgent, AppRoles.Manager));
});
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddSingleton<IAttachmentStorage, LocalAttachmentStorage>();
builder.Services.AddScoped<ITicketAttachmentService, TicketAttachmentService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<HelpDesk.Api.Application.Audit.IActivityLogService, HelpDesk.Api.Infrastructure.Audit.ActivityLogService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IReportExportService, ReportExportService>();
builder.Services.Configure<AiOptions>(builder.Configuration.GetSection(AiOptions.SectionName));
builder.Services.AddScoped<IAiTicketAnalysisService,AiTicketAnalysisService>();
builder.Services.AddHttpClient<OpenAiTicketProvider>((services, client) =>
{
    var options = services.GetRequiredService<IOptions<AiOptions>>().Value;
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 5, 300));
});
builder.Services.AddHttpClient<OllamaTicketProvider>((services, client) =>
{
    var options = services.GetRequiredService<IOptions<AiOptions>>().Value;
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 5, 300));
});
builder.Services.AddScoped<IAiTicketProvider, ConfiguredAiTicketProvider>();
builder.Services.AddScoped<ITicketLookupService, TicketLookupService>();
builder.Services.AddScoped<ISupportUserDirectoryService, SupportUserDirectoryService>();
builder.Services.AddScoped<IUserTeamManagementService, UserTeamManagementService>();
builder.Services.AddScoped<IUserRoleManagementService, UserRoleManagementService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<INotificationRealtimePublisher, SignalRNotificationRealtimePublisher>();
builder.Services.AddScoped<ITicketNotificationService, TicketNotificationService>();
builder.Services.AddSingleton<ITicketAccessContextFactory, TicketAccessContextFactory>();
builder.Services.AddSingleton<ITicketNumberGenerator, TicketNumberGenerator>();
if (builder.Environment.IsDevelopment())
{
    builder.Services.Configure<DevelopmentAdminOptions>(
        builder.Configuration.GetSection(DevelopmentAdminOptions.SectionName));
    builder.Services.AddScoped<DevelopmentAdminBootstrapper>();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<DevelopmentAdminBootstrapper>().ExecuteAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "DENY";
    context.Response.Headers.Append("Referrer-Policy", "no-referrer");
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    await next();
});
app.UseExceptionHandler();
app.UseCors("Frontend");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHealthChecks("/healthz").ExcludeFromDescription();

app.Run();

static void LoadDevelopmentDotEnv(string contentRootPath)
{
    var path = Path.Combine(contentRootPath, ".env");
    if (!File.Exists(path)) return;

    foreach (var rawLine in File.ReadLines(path))
    {
        var line = rawLine.Trim();
        if (line.Length == 0 || line.StartsWith('#')) continue;

        var separator = line.IndexOf('=');
        if (separator <= 0) continue;

        var name = line[..separator].Trim();
        if (name.StartsWith("export ", StringComparison.Ordinal)) name = name[7..].Trim();
        if (name.Length == 0 || Environment.GetEnvironmentVariable(name) is not null) continue;

        var value = line[(separator + 1)..].Trim();
        if (value.Length >= 2 && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            value = value[1..^1];
        Environment.SetEnvironmentVariable(name, value);
    }
}

static Task WriteAuthenticationProblemAsync(
    HttpContext httpContext,
    int status,
    string title,
    string detail,
    string code)
{
    var problem = new ProblemDetails
    {
        Status = status,
        Title = title,
        Detail = detail,
        Instance = httpContext.Request.Path
    };
    problem.Extensions["code"] = code;
    problem.Extensions["traceId"] = httpContext.TraceIdentifier;
    httpContext.Response.StatusCode = status;
    httpContext.Response.ContentType = "application/problem+json";
    var jsonOptions = httpContext.RequestServices
        .GetRequiredService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>()
        .Value.SerializerOptions;
    return JsonSerializer.SerializeAsync(
        httpContext.Response.Body,
        problem,
        jsonOptions,
        cancellationToken: httpContext.RequestAborted);
}

/// <summary>Exposes the application entry point to the integration-test host.</summary>
public partial class Program;
