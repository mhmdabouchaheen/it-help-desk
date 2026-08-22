using HelpDesk.Api.Application.Auth;
using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Contracts.Auth;
using HelpDesk.Api.Entities;
using HelpDesk.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net.Mail;

namespace HelpDesk.Api.Tests;

public class AuthenticationServiceTests
{
    private static readonly DateTime AccessExpiry = new(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime RefreshExpiry = new(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RegisterAsync_CreatesUser()
    {
        var fixture = new Fixture();

        await fixture.Service.RegisterAsync(RegisterRequest(), "127.0.0.1");

        fixture.UserManager.Verify(manager => manager.CreateAsync(
            It.Is<User>(user => user.Email == "employee@example.com" && user.IsActive),
            "Password1!"), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_AssignsEmployeeRole()
    {
        var fixture = new Fixture();

        await fixture.Service.RegisterAsync(RegisterRequest(), null);

        fixture.UserManager.Verify(manager => manager.AddToRoleAsync(It.IsAny<User>(), "Employee"), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsBothTokens()
    {
        var fixture = new Fixture();

        var response = await fixture.Service.RegisterAsync(RegisterRequest(), null);

        Assert.Equal("access-token", response.AccessToken);
        Assert.Equal("refresh-token", response.RefreshToken);
        Assert.Equal(AccessExpiry, response.ExpiresAtUtc);
        Assert.Equal(RefreshExpiry, response.RefreshTokenExpiresAtUtc);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmailThrowsControlledException()
    {
        var fixture = new Fixture();
        fixture.UserManager.Setup(manager => manager.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(User());

        await Assert.ThrowsAsync<EmailAlreadyRegisteredException>(
            () => fixture.Service.RegisterAsync(RegisterRequest(), null));
    }

    [Fact]
    public async Task RegisterAsync_PasswordMismatchIsRejected()
    {
        var fixture = new Fixture();
        var request = RegisterRequest();
        request.ConfirmPassword = "Different1!";

        await Assert.ThrowsAsync<UserRegistrationException>(
            () => fixture.Service.RegisterAsync(request, null));
        fixture.UserManager.Verify(
            manager => manager.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_IdentityCreationFailureThrowsControlledException()
    {
        var fixture = new Fixture();
        fixture.UserManager.Setup(manager => manager.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "PasswordTooShort" }));

        await Assert.ThrowsAsync<UserRegistrationException>(
            () => fixture.Service.RegisterAsync(RegisterRequest(), null));
    }

    [Fact]
    public async Task RegisterAsync_RoleFailureDeletesCreatedUser()
    {
        var fixture = new Fixture();
        fixture.UserManager.Setup(manager => manager.AddToRoleAsync(It.IsAny<User>(), "Employee"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "RoleNotFound" }));

        await Assert.ThrowsAsync<UserRegistrationException>(
            () => fixture.Service.RegisterAsync(RegisterRequest(), null));
        fixture.UserManager.Verify(manager => manager.DeleteAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_PassesPasswordWithoutStoringItManually()
    {
        var fixture = new Fixture();
        User? capturedUser = null;
        string? capturedPassword = null;
        fixture.UserManager.Setup(manager => manager.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .Callback<User, string>((user, password) =>
            {
                capturedUser = user;
                capturedPassword = password;
            })
            .ReturnsAsync(IdentityResult.Success);

        await fixture.Service.RegisterAsync(RegisterRequest(), null);

        Assert.Equal("Password1!", capturedPassword);
        Assert.Null(capturedUser!.PasswordHash);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentialsReturnBothTokens()
    {
        var fixture = new Fixture();
        var user = User();
        fixture.UseLoginUser(user);

        var response = await fixture.Service.LoginAsync(LoginRequest(), null);

        Assert.Equal(user.Id, response.UserId);
        Assert.Equal("access-token", response.AccessToken);
        Assert.Equal("refresh-token", response.RefreshToken);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmailThrowsGenericFailure()
    {
        var fixture = new Fixture();

        var exception = await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => fixture.Service.LoginAsync(LoginRequest(), null));

        Assert.Equal("The supplied credentials are invalid.", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_InvalidPasswordThrowsSameGenericFailure()
    {
        var fixture = new Fixture();
        fixture.UseLoginUser(User(), passwordValid: false);

        var exception = await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => fixture.Service.LoginAsync(LoginRequest(), null));

        Assert.Equal("The supplied credentials are invalid.", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_InactiveUserIsRejected()
    {
        var fixture = new Fixture();
        fixture.UseLoginUser(User(isActive: false));

        await Assert.ThrowsAsync<UserInactiveException>(
            () => fixture.Service.LoginAsync(LoginRequest(), null));
    }

    [Fact]
    public async Task LoginAsync_IncludesRoles()
    {
        var fixture = new Fixture();
        fixture.UseLoginUser(User());
        fixture.UserManager.Setup(manager => manager.GetRolesAsync(It.IsAny<User>()))
            .ReturnsAsync(["Employee", "Manager"]);

        var response = await fixture.Service.LoginAsync(LoginRequest(), null);

        Assert.Equal(["Employee", "Manager"], response.Roles);
    }

    [Fact]
    public async Task RefreshAsync_RotatesToken()
    {
        var fixture = new Fixture();
        var user = User();
        fixture.UseCurrentUser(user);
        fixture.RefreshTokens.Setup(service => service.RotateAsync(
                "old-refresh", "127.0.0.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(RefreshResult(user.Id));

        await fixture.Service.RefreshAsync(new RefreshTokenRequest { RefreshToken = "old-refresh" }, "127.0.0.1");

        fixture.RefreshTokens.Verify(service => service.RotateAsync(
            "old-refresh", "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_IssuesAccessTokenForReturnedUser()
    {
        var fixture = new Fixture();
        var user = User();
        fixture.UseCurrentUser(user);
        fixture.RefreshTokens.Setup(service => service.RotateAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RefreshResult(user.Id));

        var response = await fixture.Service.RefreshAsync(
            new RefreshTokenRequest { RefreshToken = "old-refresh" }, null);

        Assert.Equal("new-refresh-token", response.RefreshToken);
        fixture.AccessTokens.Verify(service => service.CreateAccessTokenAsync(
            user, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_MissingUserThrowsControlledException()
    {
        var fixture = new Fixture();
        fixture.RefreshTokens.Setup(service => service.RotateAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RefreshResult(Guid.NewGuid()));

        await Assert.ThrowsAsync<UserNotFoundException>(() => fixture.Service.RefreshAsync(
            new RefreshTokenRequest { RefreshToken = "old-refresh" }, null));
    }

    [Fact]
    public async Task RefreshAsync_InactiveUserIsRejected()
    {
        var fixture = new Fixture();
        var user = User(isActive: false);
        fixture.UseCurrentUser(user);
        fixture.RefreshTokens.Setup(service => service.RotateAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RefreshResult(user.Id));

        await Assert.ThrowsAsync<UserInactiveException>(() => fixture.Service.RefreshAsync(
            new RefreshTokenRequest { RefreshToken = "old-refresh" }, null));
    }

    [Fact]
    public async Task LogoutAsync_RevokesOneTokenWithLogoutReason()
    {
        var fixture = new Fixture();

        await fixture.Service.LogoutAsync(
            new LogoutRequest { RefreshToken = "refresh-token" }, "127.0.0.1");

        fixture.RefreshTokens.Verify(service => service.RevokeAsync(
            "refresh-token", "127.0.0.1", "User logout", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_DoesNotRevokeAllSessions()
    {
        var fixture = new Fixture();

        await fixture.Service.LogoutAsync(new LogoutRequest { RefreshToken = "refresh-token" }, null);

        fixture.RefreshTokens.Verify(service => service.RevokeAllForUserAsync(
            It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ReturnsSafeIdentityAndRoles()
    {
        var fixture = new Fixture();
        var user = User();
        fixture.UseCurrentUser(user);

        var response = await fixture.Service.GetCurrentUserAsync(user.Id);

        Assert.Equal(user.Id, response.UserId);
        Assert.Equal(user.Email, response.Email);
        Assert.Equal(user.DisplayName, response.DisplayName);
        Assert.Equal(["Employee"], response.Roles);
        Assert.True(response.IsActive);
    }

    [Fact]
    public async Task GetCurrentUserAsync_MissingUserThrowsControlledException()
    {
        var fixture = new Fixture();

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => fixture.Service.GetCurrentUserAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetCurrentUserAsync_EmptyIdentifierIsRejected()
    {
        var fixture = new Fixture();

        await Assert.ThrowsAsync<ArgumentException>(
            () => fixture.Service.GetCurrentUserAsync(Guid.Empty));
    }

    [Fact]
    public void AuthResponse_DoesNotExposePasswordHash()
    {
        Assert.Null(typeof(AuthResponse).GetProperty(nameof(HelpDesk.Api.Entities.User.PasswordHash)));
    }

    [Fact]
    public void CurrentUserResponse_DoesNotExposeSecurityStamps()
    {
        Assert.Null(typeof(CurrentUserResponse).GetProperty(nameof(HelpDesk.Api.Entities.User.SecurityStamp)));
        Assert.Null(typeof(CurrentUserResponse).GetProperty(nameof(HelpDesk.Api.Entities.User.ConcurrencyStamp)));
    }

    [Fact]
    public async Task ExplicitLogs_DoNotContainPasswordsOrPlaintextTokens()
    {
        const string password = "SensitivePassword1!";
        const string accessToken = "sensitive-access-token";
        const string refreshToken = "sensitive-refresh-token";
        var fixture = new Fixture();
        var request = RegisterRequest();
        request.Password = password;
        request.ConfirmPassword = password;
        fixture.AccessTokens.Setup(service => service.CreateAccessTokenAsync(
                It.IsAny<User>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(accessToken));

        await Assert.ThrowsAsync<AuthenticationTokenIssuanceException>(
            () => fixture.Service.RegisterAsync(request, null));

        var loggedText = string.Join(' ', fixture.Logger.Invocations
            .SelectMany(invocation => invocation.Arguments)
            .Where(argument => argument is not null)
            .Select(argument => argument!.ToString()));
        Assert.DoesNotContain(password, loggedText, StringComparison.Ordinal);
        Assert.DoesNotContain(accessToken, loggedText, StringComparison.Ordinal);
        Assert.DoesNotContain(refreshToken, loggedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ForgotPassword_UsesIdentityTokenAndEmailSenderOnlyForActiveUser()
    {
        var fixture = new Fixture(); var user = User(); fixture.UseLoginUser(user);
        fixture.UserManager.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("identity-token");
        await fixture.Service.ForgotPasswordAsync(new ForgotPasswordRequest { Email = user.Email! });
        fixture.PasswordResetEmails.Verify(x => x.SendPasswordResetAsync(user.Email!, "identity-token", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_SwallowsSmtpFailureWithoutLoggingSensitiveValues()
    {
        var fixture = new Fixture();
        var user = User();
        const string resetToken = "reset-token-that-must-not-be-logged";
        const string smtpPassword = "smtp-password-that-must-not-be-logged";
        fixture.UseLoginUser(user);
        fixture.UserManager.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync(resetToken);
        fixture.PasswordResetEmails
            .Setup(x => x.SendPasswordResetAsync(user.Email!, resetToken, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SmtpException($"SMTP failed for {resetToken} using {smtpPassword}"));

        await fixture.Service.ForgotPasswordAsync(new ForgotPasswordRequest { Email = user.Email! });

        var loggedText = string.Join(Environment.NewLine, fixture.Logger.Invocations.Select(invocation => invocation.Arguments[2]?.ToString()));
        Assert.Contains("Password-reset email delivery failed", loggedText, StringComparison.Ordinal);
        Assert.DoesNotContain(resetToken, loggedText, StringComparison.Ordinal);
        Assert.DoesNotContain(smtpPassword, loggedText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ForgotPassword_DoesNothingForMissingOrInactiveUser()
    {
        var fixture = new Fixture();
        await fixture.Service.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "missing@example.test" });
        fixture.UseLoginUser(User(false));
        await fixture.Service.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "inactive@example.test" });
        fixture.UserManager.Verify(x => x.GeneratePasswordResetTokenAsync(It.IsAny<User>()), Times.Never);
        fixture.PasswordResetEmails.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResetPassword_DelegatesValidationToIdentityAndRevokesRefreshTokens()
    {
        var fixture = new Fixture(); var user = User(); fixture.UseLoginUser(user);
        fixture.UserManager.Setup(x => x.ResetPasswordAsync(user, "identity-token", "Password2!")).ReturnsAsync(IdentityResult.Success);
        await fixture.Service.ResetPasswordAsync(new ResetPasswordRequest { Email = user.Email!, Token = "identity-token", NewPassword = "Password2!", ConfirmPassword = "Password2!" }, "127.0.0.1");
        fixture.RefreshTokens.Verify(x => x.RevokeAllForUserAsync(user.Id, "127.0.0.1", "Password reset", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_InvalidIdentityTokenIsSafelyRejected()
    {
        var fixture = new Fixture(); var user = User(); fixture.UseLoginUser(user);
        fixture.UserManager.Setup(x => x.ResetPasswordAsync(user, It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "InvalidToken" }));
        await Assert.ThrowsAsync<InvalidPasswordResetException>(() => fixture.Service.ResetPasswordAsync(new ResetPasswordRequest { Email = user.Email!, Token = "bad", NewPassword = "Password2!", ConfirmPassword = "Password2!" }, null));
        fixture.RefreshTokens.Verify(x => x.RevokeAllForUserAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChangePassword_UsesIdentityAndRevokesOtherSessions()
    {
        var fixture = new Fixture(); var user = User(); fixture.UseCurrentUser(user);
        fixture.UserManager.Setup(x => x.ChangePasswordAsync(user, "Password1!", "Password2!")).ReturnsAsync(IdentityResult.Success);
        await fixture.Service.ChangePasswordAsync(user.Id, new ChangePasswordRequest { CurrentPassword = "Password1!", NewPassword = "Password2!", ConfirmPassword = "Password2!" }, null);
        fixture.RefreshTokens.Verify(x => x.RevokeAllForUserAsync(user.Id, null, "Password changed", It.IsAny<CancellationToken>()), Times.Once);
    }

    private static RegisterRequest RegisterRequest() => new()
    {
        Email = " employee@example.com ",
        Password = "Password1!",
        ConfirmPassword = "Password1!",
        DisplayName = " Employee User "
    };

    private static LoginRequest LoginRequest() => new()
    {
        Email = "employee@example.com",
        Password = "Password1!"
    };

    private static User User(bool isActive = true) => new()
    {
        Id = Guid.NewGuid(),
        UserName = "employee@example.com",
        Email = "employee@example.com",
        DisplayName = "Employee User",
        IsActive = isActive
    };

    private static RefreshTokenResult RefreshResult(Guid userId) => new()
    {
        Token = "new-refresh-token",
        TokenId = Guid.NewGuid(),
        UserId = userId,
        ExpiresAtUtc = RefreshExpiry
    };

    private sealed class Fixture
    {
        public Fixture()
        {
            var store = new Mock<IUserStore<User>>();
            UserManager = new Mock<UserManager<User>>(
                store.Object,
                null!,
                Mock.Of<IPasswordHasher<User>>(),
                Array.Empty<IUserValidator<User>>(),
                Array.Empty<IPasswordValidator<User>>(),
                Mock.Of<ILookupNormalizer>(),
                new IdentityErrorDescriber(),
                Mock.Of<IServiceProvider>(),
                Mock.Of<ILogger<UserManager<User>>>());
            AccessTokens = new Mock<IAccessTokenService>();
            RefreshTokens = new Mock<IRefreshTokenService>();
            Logger = new Mock<ILogger<AuthenticationService>>();
            PasswordResetEmails = new Mock<IPasswordResetEmailSender>();

            UserManager.Setup(manager => manager.NormalizeEmail(It.IsAny<string>()))
                .Returns((string email) => email.ToUpperInvariant());
            UserManager.Setup(manager => manager.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);
            UserManager.Setup(manager => manager.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            UserManager.Setup(manager => manager.AddToRoleAsync(It.IsAny<User>(), "Employee"))
                .ReturnsAsync(IdentityResult.Success);
            UserManager.Setup(manager => manager.DeleteAsync(It.IsAny<User>()))
                .ReturnsAsync(IdentityResult.Success);
            UserManager.Setup(manager => manager.GetRolesAsync(It.IsAny<User>()))
                .ReturnsAsync(["Employee"]);
            AccessTokens.Setup(service => service.CreateAccessTokenAsync(
                    It.IsAny<User>(),
                    It.IsAny<IReadOnlyCollection<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessTokenResult { Token = "access-token", ExpiresAtUtc = AccessExpiry });
            RefreshTokens.Setup(service => service.CreateAsync(
                    It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid userId, string? _, CancellationToken _) => new RefreshTokenResult
                {
                    Token = "refresh-token",
                    TokenId = Guid.NewGuid(),
                    UserId = userId,
                    ExpiresAtUtc = RefreshExpiry
                });
            RefreshTokens.Setup(service => service.RevokeAsync(
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            RefreshTokens.Setup(service => service.RevokeAllForUserAsync(
                    It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            PasswordResetEmails.Setup(x => x.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            Service = new AuthenticationService(
                UserManager.Object,
                AccessTokens.Object,
                RefreshTokens.Object,
                Logger.Object,
                passwordResetEmailSender: PasswordResetEmails.Object);
        }

        public Mock<UserManager<User>> UserManager { get; }
        public Mock<IAccessTokenService> AccessTokens { get; }
        public Mock<IRefreshTokenService> RefreshTokens { get; }
        public Mock<ILogger<AuthenticationService>> Logger { get; }
        public Mock<IPasswordResetEmailSender> PasswordResetEmails { get; }
        public AuthenticationService Service { get; }

        public void UseLoginUser(User user, bool passwordValid = true)
        {
            UserManager.Setup(manager => manager.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            UserManager.Setup(manager => manager.CheckPasswordAsync(user, It.IsAny<string>()))
                .ReturnsAsync(passwordValid);
        }

        public void UseCurrentUser(User user)
        {
            UserManager.Setup(manager => manager.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        }
    }
}
