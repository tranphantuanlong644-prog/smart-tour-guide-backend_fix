using Microsoft.AspNetCore.Authorization;
using SmartTourGuide.API.Controllers;

namespace SmartTourGuide.Tests;

public sealed class AdminControllerAuthorizationTests
{
    [Fact]
    public void Controller_RequiresAdministratorRole()
    {
        var authorization = Assert.Single(
            typeof(AdminController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>());

        Assert.Equal("Administrator", authorization.Roles);
    }
}
