using Microsoft.AspNetCore.Mvc;

namespace Gatekeeper.Api.Controllers;

[ApiController]
public class GatekeeperController : ControllerBase
{
    public const string ApiKeyHeaderName = "X-Gatekeeper-Key";

    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<GatekeeperController> _logger;

    public GatekeeperController(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<GatekeeperController> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet("/public/ping")]
    public IActionResult PublicPing()
    {
        return Ok(new
        {
            status = "ok",
            gate = "public",
            requestId = RequestId
        });
    }

    [HttpGet("/version")]
    public IActionResult Version()
    {
        return Ok(new
        {
            service = "Gatekeeper.Api",
            version = typeof(GatekeeperController).Assembly.GetName().Version?.ToString() ?? "unknown",
            environment = _environment.EnvironmentName,
            commit = FirstEnvironmentValue("GITHUB_SHA", "COMMIT_SHA", "APP_COMMIT") ?? "local",
            run = FirstEnvironmentValue("GITHUB_RUN_ID", "APP_RUN_ID") ?? "local",
            requestId = RequestId
        });
    }

    [HttpGet("/secure/treasure")]
    public IActionResult SecureTreasure()
    {
        var expectedKey = _configuration["Gatekeeper:ApiKey"];

        if (string.IsNullOrWhiteSpace(expectedKey))
        {
            _logger.LogWarning("Protected gate is not configured. RequestId: {RequestId}", RequestId);

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "gate_not_configured",
                message = "Gatekeeper:ApiKey is not configured.",
                requestId = RequestId
            });
        }

        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var submittedKey) ||
            !string.Equals(submittedKey.ToString(), expectedKey, StringComparison.Ordinal))
        {
            _logger.LogWarning("Protected gate blocked a request. RequestId: {RequestId}", RequestId);

            return Unauthorized(new
            {
                error = "unauthorized",
                message = $"Missing or invalid {ApiKeyHeaderName} header.",
                requiredHeader = ApiKeyHeaderName,
                requestId = RequestId
            });
        }

        return Ok(new
        {
            message = "Treasure unlocked.",
            treasure = "Deploy, CI/CD, gateway, logs, and debugging.",
            requestId = RequestId
        });
    }

    [HttpGet("/chaos/error")]
    public IActionResult ChaosError()
    {
        _logger.LogWarning("Intentional error requested. RequestId: {RequestId}", RequestId);

        throw new InvalidOperationException("Intentional chaos error for log/debug demo.");
    }

    [HttpGet("/chaos/slow")]
    public async Task<IActionResult> ChaosSlow([FromQuery] int ms = 1500, CancellationToken cancellationToken = default)
    {
        if (ms < 0 || ms > 10_000)
        {
            return BadRequest(new
            {
                error = "invalid_delay",
                message = "Query parameter 'ms' must be between 0 and 10000.",
                requestId = RequestId
            });
        }

        await Task.Delay(ms, cancellationToken);

        return Ok(new
        {
            status = "completed",
            requestedDelayMs = ms,
            requestId = RequestId
        });
    }

    private string RequestId => HttpContext.TraceIdentifier;

    private static string? FirstEnvironmentValue(params string[] names)
    {
        foreach (var name in names)
        {
            var value = Environment.GetEnvironmentVariable(name);

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
