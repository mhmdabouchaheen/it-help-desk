using System.Security.Claims;
using System.Text;
using System.Text.Json;
using HelpDesk.Api.Application.Auth;
using HelpDesk.Api.Application.Attachments;
using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Application.Tickets;
using HelpDesk.Api.Application.Users;
using HelpDesk.Api.Configuration;
using HelpDesk.Api.Data;
using HelpDesk.Api.Entities;
using HelpDesk.Api.Infrastructure.Auth;
using HelpDesk.Api.Infrastructure.Attachments;
using HelpDesk.Api.Infrastructure.Authorization;
using HelpDesk.Api.Infrastructure.ExceptionHandling;
using HelpDesk.Api.Infrastructure.Tickets;
using HelpDesk.Api.Infrastructure.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
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
builder.Services.AddOptions<AttachmentOptions>()
    .Bind(builder.Configuration.GetSection(AttachmentOptions.SectionName))
    .Validate(x => !string.IsNullOrWhiteSpace(x.StorageRoot), "Attachment storage root must not be blank.")
    .Validate(x => x.MaxFileSizeBytes > 0, "Attachment maximum size must be positive.")
    .Validate(x => x.AllowedContentTypes.Length > 0 && x.AllowedContentTypes.All(v => !string.IsNullOrWhiteSpace(v) && !v.Contains('*')), "Attachment content types must be explicit.")
    .Validate(x => x.AllowedExtensions.Length > 0, "Attachment extensions must not be empty.")
    .Validate(x => x.AllowedExtensions.All(v => new[] { ".png", ".jpg", ".jpeg", ".webp", ".pdf", ".txt", ".docx", ".xlsx" }.Contains(v.StartsWith('.') ? v.ToLowerInvariant() : "." + v.ToLowerInvariant())), "Attachment extensions contain an unsafe or unsupported value.")
    .ValidateOnStart();
var frontendOrigins = builder.Configuration.GetSection("Frontend:AllowedOrigins").Get<string[]>() ?? [];
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
});
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddSingleton<IAttachmentStorage, LocalAttachmentStorage>();
builder.Services.AddScoped<ITicketAttachmentService, TicketAttachmentService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<ITicketLookupService, TicketLookupService>();
builder.Services.AddScoped<ISupportUserDirectoryService, SupportUserDirectoryService>();
builder.Services.AddSingleton<ITicketAccessContextFactory, TicketAccessContextFactory>();
builder.Services.AddSingleton<ITicketNumberGenerator, TicketNumberGenerator>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("Frontend");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

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

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

/// <summary>Exposes the application entry point to the integration-test host.</summary>
public partial class Program;
