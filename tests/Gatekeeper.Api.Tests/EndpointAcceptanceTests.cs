using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Gatekeeper.Api;
using Gatekeeper.Api.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Gatekeeper.Api.Tests;

public class EndpointAcceptanceTests : IClassFixture<GatekeeperApiFixture>
{
    private readonly GatekeeperApiFixture _fixture;

    public EndpointAcceptanceTests(GatekeeperApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PublicPing_ReturnsHealthCheckWithRequestId()
    {
        var response = await _fixture.Client.GetAsync("/public/ping");
        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ok", RequiredString(json, "status"));
        Assert.Equal("public", RequiredString(json, "gate"));
        AssertHasRequestId(json);
    }

    [Fact]
    public async Task Version_ReturnsDeploymentFingerprint()
    {
        var response = await _fixture.Client.GetAsync("/version");
        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Gatekeeper.Api", RequiredString(json, "service"));
        Assert.False(string.IsNullOrWhiteSpace(RequiredString(json, "version")));
        Assert.Equal("AcceptanceTest", RequiredString(json, "environment"));
        Assert.False(string.IsNullOrWhiteSpace(RequiredString(json, "commit")));
        Assert.False(string.IsNullOrWhiteSpace(RequiredString(json, "run")));
        AssertHasRequestId(json);
    }

    [Fact]
    public async Task SecureTreasure_WithoutApiKey_IsBlocked()
    {
        var response = await _fixture.Client.GetAsync("/secure/treasure");
        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("unauthorized", RequiredString(json, "error"));
        Assert.Equal(GatekeeperController.ApiKeyHeaderName, RequiredString(json, "requiredHeader"));
        Assert.Contains(GatekeeperController.ApiKeyHeaderName, RequiredString(json, "message"));
        AssertHasRequestId(json);
    }

    [Fact]
    public async Task SecureTreasure_WithValidApiKey_IsUnlocked()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/secure/treasure");
        request.Headers.Add(GatekeeperController.ApiKeyHeaderName, GatekeeperApiFixture.ApiKey);

        var response = await _fixture.Client.SendAsync(request);
        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Treasure unlocked.", RequiredString(json, "message"));
        Assert.Contains("CI/CD", RequiredString(json, "treasure"));
        AssertHasRequestId(json);
    }

    [Fact]
    public async Task ChaosError_ReturnsInternalServerErrorWithRequestId()
    {
        var response = await _fixture.Client.GetAsync("/chaos/error");
        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("internal_server_error", RequiredString(json, "error"));
        Assert.Contains("Intentional failure", RequiredString(json, "message"));
        AssertHasRequestId(json);
    }

    [Fact]
    public async Task ChaosSlow_ReturnsControlledLatencyWithRequestId()
    {
        const int delayMs = 150;
        var stopwatch = Stopwatch.StartNew();

        var response = await _fixture.Client.GetAsync($"/chaos/slow?ms={delayMs}");

        stopwatch.Stop();

        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("completed", RequiredString(json, "status"));
        Assert.Equal(delayMs, RequiredInt32(json, "requestedDelayMs"));
        Assert.True(
            stopwatch.ElapsedMilliseconds >= 100,
            $"Expected a controlled delay near {delayMs}ms, actual: {stopwatch.ElapsedMilliseconds}ms.");
        Assert.True(
            stopwatch.ElapsedMilliseconds < 3_000,
            $"Expected slow endpoint to finish within a controlled limit, actual: {stopwatch.ElapsedMilliseconds}ms.");
        AssertHasRequestId(json);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        return document.RootElement.Clone();
    }

    private static void AssertHasRequestId(JsonElement json)
    {
        var requestId = RequiredString(json, "requestId");

        Assert.Contains(':', requestId);
    }

    private static string RequiredString(JsonElement json, string propertyName)
    {
        Assert.True(json.TryGetProperty(propertyName, out var property), $"Missing JSON property '{propertyName}'.");
        Assert.Equal(JsonValueKind.String, property.ValueKind);

        var value = property.GetString();

        Assert.False(string.IsNullOrWhiteSpace(value), $"JSON property '{propertyName}' must not be empty.");

        return value;
    }

    private static int RequiredInt32(JsonElement json, string propertyName)
    {
        Assert.True(json.TryGetProperty(propertyName, out var property), $"Missing JSON property '{propertyName}'.");
        Assert.Equal(JsonValueKind.Number, property.ValueKind);

        return property.GetInt32();
    }
}

public sealed class GatekeeperApiFixture : IAsyncLifetime
{
    public const string ApiKey = "acceptance-test-key";

    private WebApplication? _app;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _app = GatekeeperApplication.Create(
            new WebApplicationOptions
            {
                ApplicationName = typeof(GatekeeperApplication).Assembly.GetName().Name,
                EnvironmentName = "AcceptanceTest"
            },
            new Dictionary<string, string?>
            {
                ["Gatekeeper:ApiKey"] = ApiKey
            });

        _app.Urls.Add("http://127.0.0.1:0");

        await _app.StartAsync();

        var server = _app.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses
            ?? throw new InvalidOperationException("Server did not expose a listening address.");
        var address = Assert.Single(addresses);

        Client = new HttpClient
        {
            BaseAddress = new Uri(address)
        };
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();

        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
