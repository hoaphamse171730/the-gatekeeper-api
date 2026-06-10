using Microsoft.AspNetCore.Diagnostics;

namespace Gatekeeper.Api;

public static class GatekeeperApplication
{
    public static WebApplication Create(string[] args)
    {
        return Create(new WebApplicationOptions
        {
            Args = args,
            ApplicationName = typeof(GatekeeperApplication).Assembly.GetName().Name
        });
    }

    public static WebApplication Create(
        WebApplicationOptions options,
        IDictionary<string, string?>? inMemorySettings = null)
    {
        var builder = WebApplication.CreateBuilder(options);

        if (inMemorySettings is not null)
        {
            builder.Configuration.AddInMemoryCollection(inMemorySettings);
        }

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var requestId = context.TraceIdentifier;
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                var logger = context.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Gatekeeper.Api.ErrorHandler");

                if (exception is not null)
                {
                    logger.LogError(exception, "Unhandled exception. RequestId: {RequestId}", requestId);
                }

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(new
                {
                    error = "internal_server_error",
                    message = "Intentional failure triggered for debug demo. Check logs with requestId.",
                    requestId
                });
            });
        });

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}
