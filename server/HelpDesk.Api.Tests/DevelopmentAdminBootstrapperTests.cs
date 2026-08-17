using HelpDesk.Api.Application.Authorization;
using HelpDesk.Api.Configuration;
using HelpDesk.Api.Entities;
using HelpDesk.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace HelpDesk.Api.Tests;

public sealed class DevelopmentAdminBootstrapperTests
{
    [Fact]
    public async Task DisabledConfiguration_PerformsNoIdentityWork()
    {
        var fixture = new Fixture(options: new() { Enabled = false });

        await fixture.Service.ExecuteAsync();

        fixture.Users.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        fixture.Roles.Verify(x => x.RoleExistsAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task NonDevelopmentEnvironment_PerformsNoIdentityWorkEvenWhenEnabled()
    {
        var fixture = new Fixture(environmentName: "Production");

        await fixture.Service.ExecuteAsync();

        fixture.Users.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        fixture.Roles.Verify(x => x.RoleExistsAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task MissingUser_IsCreatedWithConfiguredPasswordAndNoManualHash()
    {
        var fixture = new Fixture();

        await fixture.Service.ExecuteAsync();

        fixture.Users.Verify(x => x.CreateAsync(
            It.Is<User>(user => user.Id != Guid.Empty && user.Email == Fixture.Email &&
                user.UserName == Fixture.Email && user.DisplayName == Fixture.DisplayName &&
                user.IsActive && user.PasswordHash == null),
            Fixture.Password), Times.Once);
    }

    [Fact]
    public async Task MissingUser_ReceivesExactAdminRole()
    {
        var fixture = new Fixture();

        await fixture.Service.ExecuteAsync();

        fixture.Roles.Verify(x => x.RoleExistsAsync(AppRoles.Admin), Times.Once);
        fixture.Users.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), AppRoles.Admin), Times.Once);
    }

    [Fact]
    public async Task ExistingUser_IsNotCreatedOrPasswordReset()
    {
        var fixture = new Fixture(existingUser: Fixture.User());

        await fixture.Service.ExecuteAsync();

        fixture.Users.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        fixture.Users.Verify(x => x.ResetPasswordAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        fixture.Users.Verify(x => x.ChangePasswordAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExistingNonAdmin_ReceivesAdminRoleWithoutDataChanges()
    {
        var user = Fixture.User();
        var originalName = user.DisplayName;
        var fixture = new Fixture(existingUser: user);

        await fixture.Service.ExecuteAsync();

        fixture.Users.Verify(x => x.AddToRoleAsync(user, AppRoles.Admin), Times.Once);
        Assert.Equal(originalName, user.DisplayName);
    }

    [Fact]
    public async Task ExistingAdminMembership_IsNotDuplicated()
    {
        var user = Fixture.User();
        var fixture = new Fixture(existingUser: user, isAdmin: true);

        await fixture.Service.ExecuteAsync();

        fixture.Users.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("", Fixture.Password, Fixture.DisplayName, "email")]
    [InlineData(Fixture.Email, "", Fixture.DisplayName, "password")]
    [InlineData(Fixture.Email, Fixture.Password, "", "display name")]
    public async Task MissingRequiredConfiguration_IsRejectedSafely(
        string email, string password, string displayName, string expectedField)
    {
        var fixture = new Fixture(options: new()
        {
            Enabled = true,
            Email = email,
            Password = password,
            DisplayName = displayName
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ExecuteAsync());

        Assert.Contains(expectedField, exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Fixture.Password, exception.Message, StringComparison.Ordinal);
        fixture.Users.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task PasswordPolicyFailure_IsRejectedBeforeIdentityLookup()
    {
        var validator = new Mock<IPasswordValidator<User>>();
        validator.Setup(x => x.ValidateAsync(It.IsAny<UserManager<User>>(), It.IsAny<User>(), Fixture.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "PasswordTooShort" }));
        var fixture = new Fixture(passwordValidators: [validator.Object]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ExecuteAsync());

        Assert.Contains("password policy", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Fixture.Password, exception.Message, StringComparison.Ordinal);
        fixture.Users.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task IdentityCreationFailure_DoesNotExposePasswordInExceptionOrLogs()
    {
        var logger = new RecordingLogger<DevelopmentAdminBootstrapper>();
        var fixture = new Fixture(logger: logger);
        fixture.Users.Setup(x => x.CreateAsync(It.IsAny<User>(), Fixture.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordTooShort",
                Description = $"Rejected {Fixture.Password}"
            }));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ExecuteAsync());

        Assert.DoesNotContain(Fixture.Password, exception.ToString(), StringComparison.Ordinal);
        Assert.All(logger.Messages, message => Assert.DoesNotContain(Fixture.Password, message, StringComparison.Ordinal));
        Assert.Contains(logger.Messages, message => message.Contains("PasswordTooShort", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MissingSeededAdminRole_FailsWithoutCreatingRoleOrUser()
    {
        var fixture = new Fixture(roleExists: false);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ExecuteAsync());

        Assert.Contains(AppRoles.Admin, exception.Message, StringComparison.Ordinal);
        fixture.Roles.Verify(x => x.CreateAsync(It.IsAny<Role>()), Times.Never);
        fixture.Users.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RepeatedExecution_IsIdempotent()
    {
        User? storedUser = null;
        var isAdmin = false;
        var fixture = new Fixture();
        fixture.Users.Setup(x => x.FindByEmailAsync(Fixture.Email)).ReturnsAsync(() => storedUser);
        fixture.Users.Setup(x => x.CreateAsync(It.IsAny<User>(), Fixture.Password))
            .Callback<User, string>((user, _) => storedUser = user)
            .ReturnsAsync(IdentityResult.Success);
        fixture.Users.Setup(x => x.IsInRoleAsync(It.IsAny<User>(), AppRoles.Admin)).ReturnsAsync(() => isAdmin);
        fixture.Users.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), AppRoles.Admin))
            .Callback(() => isAdmin = true)
            .ReturnsAsync(IdentityResult.Success);

        await fixture.Service.ExecuteAsync();
        await fixture.Service.ExecuteAsync();

        fixture.Users.Verify(x => x.CreateAsync(It.IsAny<User>(), Fixture.Password), Times.Once);
        fixture.Users.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), AppRoles.Admin), Times.Once);
    }

    private sealed class Fixture
    {
        public const string Email = "admin@example.test";
        public const string Password = "Local-Only-Password-42!";
        public const string DisplayName = "Local Admin";

        public Fixture(
            DevelopmentAdminOptions? options = null,
            string environmentName = "Development",
            User? existingUser = null,
            bool isAdmin = false,
            bool roleExists = true,
            IEnumerable<IPasswordValidator<User>>? passwordValidators = null,
            ILogger<DevelopmentAdminBootstrapper>? logger = null)
        {
            var userStore = new Mock<IUserStore<User>>();
            Users = new Mock<UserManager<User>>(
                userStore.Object,
                Options.Create(new IdentityOptions()),
                Mock.Of<IPasswordHasher<User>>(),
                Array.Empty<IUserValidator<User>>(),
                passwordValidators ?? Array.Empty<IPasswordValidator<User>>(),
                Mock.Of<ILookupNormalizer>(),
                new IdentityErrorDescriber(),
                Mock.Of<IServiceProvider>(),
                Mock.Of<ILogger<UserManager<User>>>());
            var roleStore = new Mock<IRoleStore<Role>>();
            Roles = new Mock<RoleManager<Role>>(
                roleStore.Object,
                Array.Empty<IRoleValidator<Role>>(),
                Mock.Of<ILookupNormalizer>(),
                new IdentityErrorDescriber(),
                Mock.Of<ILogger<RoleManager<Role>>>());

            Users.Setup(x => x.FindByEmailAsync(Email)).ReturnsAsync(existingUser);
            Users.Setup(x => x.CreateAsync(It.IsAny<User>(), Password)).ReturnsAsync(IdentityResult.Success);
            Users.Setup(x => x.IsInRoleAsync(It.IsAny<User>(), AppRoles.Admin)).ReturnsAsync(isAdmin);
            Users.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), AppRoles.Admin)).ReturnsAsync(IdentityResult.Success);
            Roles.Setup(x => x.RoleExistsAsync(AppRoles.Admin)).ReturnsAsync(roleExists);

            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(x => x.EnvironmentName).Returns(environmentName);
            Service = new DevelopmentAdminBootstrapper(
                Users.Object,
                Roles.Object,
                Options.Create(options ?? EnabledOptions()),
                logger ?? Mock.Of<ILogger<DevelopmentAdminBootstrapper>>(),
                environment.Object);
        }

        public Mock<UserManager<User>> Users { get; }
        public Mock<RoleManager<Role>> Roles { get; }
        public DevelopmentAdminBootstrapper Service { get; }

        public static User User() => new()
        {
            Id = Guid.NewGuid(),
            Email = Email,
            UserName = Email,
            DisplayName = "Existing User",
            IsActive = true
        };

        private static DevelopmentAdminOptions EnabledOptions() => new()
        {
            Enabled = true,
            Email = Email,
            Password = Password,
            DisplayName = DisplayName
        };
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
    }
}
