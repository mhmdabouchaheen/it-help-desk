using HelpDesk.Api.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace HelpDesk.Api.Tests;

public class AuthorizationPolicyRegistrationTests
{
    [Fact]
    public async Task AuthenticatedUserPolicy_Exists() =>
        Assert.NotNull(await PolicyAsync(AppPolicies.AuthenticatedUser));

    [Fact]
    public async Task AdminOnlyPolicy_Exists() =>
        Assert.NotNull(await PolicyAsync(AppPolicies.AdminOnly));

    [Fact]
    public async Task SupportStaffPolicy_Exists() =>
        Assert.NotNull(await PolicyAsync(AppPolicies.SupportStaff));

    [Fact]
    public async Task ManagementPolicy_Exists() =>
        Assert.NotNull(await PolicyAsync(AppPolicies.Management));

    [Fact]
    public async Task AuthenticatedUser_RequiresAuthenticatedPrincipal()
    {
        var policy = await PolicyAsync(AppPolicies.AuthenticatedUser);
        Assert.Contains(policy!.Requirements, requirement =>
            requirement is DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public async Task AdminOnly_AllowsOnlyAdmin() =>
        Assert.Equal([AppRoles.Admin], await AllowedRolesAsync(AppPolicies.AdminOnly));

    [Fact]
    public async Task SupportStaff_AllowsExactlyExpectedRoles() =>
        Assert.Equal(
            [AppRoles.Admin, AppRoles.ItSupportAgent],
            await AllowedRolesAsync(AppPolicies.SupportStaff));

    [Fact]
    public async Task Management_AllowsExactlyExpectedRoles() =>
        Assert.Equal(
            [AppRoles.Admin, AppRoles.Manager],
            await AllowedRolesAsync(AppPolicies.Management));

    [Fact]
    public async Task RestrictedPolicies_DoNotAllowEmployee()
    {
        Assert.DoesNotContain(AppRoles.Employee, await AllowedRolesAsync(AppPolicies.AdminOnly));
        Assert.DoesNotContain(AppRoles.Employee, await AllowedRolesAsync(AppPolicies.SupportStaff));
        Assert.DoesNotContain(AppRoles.Employee, await AllowedRolesAsync(AppPolicies.Management));
    }

    [Fact]
    public async Task Management_DoesNotAllowItSupportAgent() =>
        Assert.DoesNotContain(AppRoles.ItSupportAgent, await AllowedRolesAsync(AppPolicies.Management));

    private static async Task<AuthorizationPolicy?> PolicyAsync(string name)
    {
        await using var factory = new AuthApiFactory();
        var provider = factory.Services.GetRequiredService<IAuthorizationPolicyProvider>();
        return await provider.GetPolicyAsync(name);
    }

    private static async Task<string[]> AllowedRolesAsync(string policyName)
    {
        var policy = await PolicyAsync(policyName);
        return policy!.Requirements
            .OfType<RolesAuthorizationRequirement>()
            .Single()
            .AllowedRoles
            .ToArray();
    }
}
