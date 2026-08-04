using HelpDesk.Api.Application.Users;
using HelpDesk.Api.Contracts.Users;
using HelpDesk.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HelpDesk.Api.Tests;

public sealed class SupportUsersControllerTests
{
    [Fact]
    public async Task Get_DelegatesOnce_ForwardsToken_AndReturnsOkCollection()
    {
        var token = new CancellationTokenSource().Token;
        IReadOnlyList<SupportUserResponse> expected = [new() { Id = Guid.NewGuid(), DisplayName = "Agent" }];
        var service = new Mock<ISupportUserDirectoryService>();
        service.Setup(x => x.GetEligibleSupportUsersAsync(token)).ReturnsAsync(expected);
        var result = await new SupportUsersController(service.Object).GetAsync(token);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
        service.Verify(x => x.GetEligibleSupportUsersAsync(token), Times.Once);
    }

    [Fact]
    public void Constructor_DependsOnlyOnDirectoryService()
    {
        var parameter = Assert.Single(typeof(SupportUsersController).GetConstructors().Single().GetParameters());
        Assert.Equal(typeof(ISupportUserDirectoryService), parameter.ParameterType);
    }
}
