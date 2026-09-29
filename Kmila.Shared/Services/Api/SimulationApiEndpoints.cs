#if KMILA_HAS_ASPNETCORE
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services.Api;

// Minimal-API endpoint group for the headless simulation surface. Mounted
// by Kmila.Web's Program.cs via `app.MapSimulationApi()`. The endpoint
// bodies delegate to SimulationApiHandlers so the MAUI-embedded HttpListener
// host (Kmila/Services/EmbeddedApiHost.cs) can call the same code paths
// without duplicating the logic. Localhost-only by host configuration.
/// <summary>
/// Minimal-API endpoint group for the headless simulation surface, mounted by
/// Kmila.Web via <see cref="MapSimulationApi"/>. Endpoint bodies delegate to
/// <c>SimulationApiHandlers</c> so the MAUI-embedded host shares the same code.
/// </summary>
public static class SimulationApiEndpoints
{
    /// <summary>Base URL path the simulation API is mounted under (re-exported from the handlers).</summary>
    // BasePath moved to SimulationApiHandlers (target-agnostic) so the MAUI
    // HttpListener host can reach it. Kept here as a re-export for backwards
    // compatibility.
    public const string BasePath = SimulationApiHandlers.BasePath;

    /// <summary>Registers the simulation API's services (sandbox store, simulator, janitor).</summary>
    /// <param name="services">The service collection to add to.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddSimulationApi(this IServiceCollection services)
    {
        // TryAdd so a caller that pre-registered a configured SandboxProjectStore
        // (e.g. EmbeddedApiHost in the MAUI head, which needs a
        // FileSystem.AppDataDirectory-rooted store) wins over the default.
        services.TryAddSingleton<SandboxProjectStore>();
        services.TryAddSingleton<HeadlessSimulator>();
        // Cross-host bearer-token holder for the /api/v1 auth filter.
        // TryAdd so MauiProgram can pre-register with a hydrated token.
        services.TryAddSingleton<LocalApiTokenProvider>();
        // Background GC — reaps expired sandboxes + on-disk orphans left by
        // past process crashes. See SandboxJanitor for the TTL policy.
        services.AddHostedService<SandboxJanitor>();
        return services;
    }

    /// <summary>Maps the simulation API endpoints, including the bearer-token auth filter.</summary>
    /// <param name="app">The endpoint route builder to map onto.</param>
    /// <returns>The same route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapSimulationApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(BasePath).WithTags("SimulationApi");

        // Tier 5.2 Phase B follow-up — bearer-token gate. Empty token in
        // the provider means "auth disabled" (dev-parity default). /health
        // stays exempt so external tooling can probe reachability without
        // knowing the secret.
        group.AddEndpointFilter(async (ctx, next) =>
        {
            var provider = ctx.HttpContext.RequestServices.GetRequiredService<LocalApiTokenProvider>();
            var expected = provider.Current;
            if (string.IsNullOrEmpty(expected)) return await next(ctx);

            var path = ctx.HttpContext.Request.Path.Value ?? string.Empty;
            if (string.Equals(path, BasePath + "/health", StringComparison.Ordinal))
                return await next(ctx);

            var headers  = ctx.HttpContext.Request.Headers;
            var verdict  = SimulationApiHandlers.CheckAuth(
                expected,
                name => headers.TryGetValue(name, out var v) ? v.ToString() : null);
            if (verdict is not null) return ToResult(verdict);

            return await next(ctx);
        });

        group.MapGet("/health", () => ToResult(SimulationApiHandlers.Health()));

        group.MapPost("/projects", (CreateProjectRequest? req, SandboxProjectStore store) =>
            ToResult(SimulationApiHandlers.CreateProject(req, store)));

        group.MapPost("/projects/{id}/files", (string id, WriteFileRequest body, SandboxProjectStore store) =>
            ToResult(SimulationApiHandlers.WriteFile(id, body, store)));

        group.MapGet("/projects/{id}/files/{*name}", (string id, string name, SandboxProjectStore store) =>
            ToResult(SimulationApiHandlers.ReadFile(id, name, store)));

        group.MapDelete("/projects/{id}", (string id, SandboxProjectStore store) =>
            ToResult(SimulationApiHandlers.DeleteProject(id, store)));

        group.MapPost("/simulate", async (SimulateRequest req, HeadlessSimulator sim, ILoggerFactory lf) =>
            ToResult(await SimulationApiHandlers.SimulateAsync(req, sim, lf.CreateLogger("SimulationApi"))));

        return app;
    }

    // Bridge the transport-neutral ApiResponse into a Minimal-API IResult.
    // Kept trivial — the endpoint bodies produce the final status/content/body,
    // this only marshals them into ASP.NET Core's pipeline.
    private static IResult ToResult(ApiResponse r) =>
        r.StatusCode == 204
            ? Results.NoContent()
            : Results.Content(r.Body, r.ContentType, System.Text.Encoding.UTF8, r.StatusCode);
}
#endif
