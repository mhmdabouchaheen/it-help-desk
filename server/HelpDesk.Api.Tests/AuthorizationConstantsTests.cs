using HelpDesk.Api.Application.Authorization;

namespace HelpDesk.Api.Tests;

public class AuthorizationConstantsTests
{
    [Fact]
    public void Admin_MatchesSeededRole() => Assert.Equal("Admin", AppRoles.Admin);

    [Fact]
    public void ItSupportAgent_MatchesSeededRole() =>
        Assert.Equal("IT Support Agent", AppRoles.ItSupportAgent);

    [Fact]
    public void Employee_MatchesSeededRole() => Assert.Equal("Employee", AppRoles.Employee);

    [Fact]
    public void Manager_MatchesSeededRole() => Assert.Equal("Manager", AppRoles.Manager);

    [Fact]
    public void All_ContainsExactlyFourUniqueRoles()
    {
        Assert.Equal(4, AppRoles.All.Count);
        Assert.Equal(4, AppRoles.All.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(
            [AppRoles.Admin, AppRoles.ItSupportAgent, AppRoles.Employee, AppRoles.Manager],
            AppRoles.All);
    }

    [Fact]
    public void SupportStaff_ContainsExpectedRoles() =>
        Assert.Equal([AppRoles.Admin, AppRoles.ItSupportAgent], AppRoles.SupportStaff);

    [Fact]
    public void Management_ContainsExpectedRoles() =>
        Assert.Equal([AppRoles.Admin, AppRoles.Manager], AppRoles.Management);

    [Fact]
    public void RoleGroups_CannotBeMutatedThroughPublicApi()
    {
        foreach (var group in new[] { AppRoles.All, AppRoles.SupportStaff, AppRoles.Management })
        {
            var collection = Assert.IsAssignableFrom<ICollection<string>>(group);
            Assert.True(collection.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => collection.Add("Unexpected"));
        }
    }

    [Fact]
    public void PolicyNames_AreUniqueAndNonblank()
    {
        string[] policies =
        [
            AppPolicies.AuthenticatedUser,
            AppPolicies.AdminOnly,
            AppPolicies.SupportStaff,
            AppPolicies.Management
        ];

        Assert.All(policies, policy => Assert.False(string.IsNullOrWhiteSpace(policy)));
        Assert.Equal(policies.Length, policies.Distinct(StringComparer.Ordinal).Count());
    }
}
