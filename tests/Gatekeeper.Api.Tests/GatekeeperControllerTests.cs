using Gatekeeper.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Gatekeeper.Api.Tests;

public class GatekeeperControllerTests
{
    [Fact]
    public void ExposesExpectedGetRoutes()
    {
        var routes = typeof(GatekeeperController)
            .GetMethods()
            .SelectMany(method => method.GetCustomAttributes(typeof(HttpGetAttribute), inherit: false))
            .Cast<HttpGetAttribute>()
            .Select(attribute => attribute.Template)
            .OrderBy(route => route)
            .ToArray();

        Assert.Equal(
            [
                "/chaos/error",
                "/chaos/slow",
                "/public/ping",
                "/secure/treasure",
                "/version"
            ],
            routes);
    }
}
