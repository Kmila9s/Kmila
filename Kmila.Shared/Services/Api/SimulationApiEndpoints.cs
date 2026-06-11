#if ASPNETCORE
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kmila.Shared.Services.Api;

// Minimal-API endpoint group for the headless simulation surface. Mounted
// by Kmila.Web's Program.cs (and Phase B's MAUI embedded Kestrel) via
// `app.MapSimulationApi()`. Localhost-only by host configuration.
public static class SimulationApiEndpoints
{
    public const string BasePath = "/api/v1";

    public static IServiceCollection AddSimulationApi(this IServiceCollection services)
    {
        services.AddSingleton<SandboxProjectStore>();
        services.AddSingleton<HeadlessSimulator>();
        return services;
    }

    public static IEndpointRouteBuilder MapSimulationApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(BasePath).WithTags("SimulationApi");

        group.MapGet("/health", () => Results.Ok(new
        {
            status     = "ok",
            version    = "1",
            startedUtc = DateTime.UtcNow,
        }));

        group.MapPost("/projects", (CreateProjectRequest? req, SandboxProjectStore store) =>
        {
            req ??= new CreateProjectRequest();
            var p = store.Create(req.Name, req.Retain);
            return Results.Ok(new CreateProjectResponse { ProjectId = p.Id, SandboxPath = p.Path });
        });

        group.MapPost("/projects/{id}/files", (string id, WriteFileRequest body, SandboxProjectStore store) =>
        {
            var project = store.Get(id);
            if (project == null) return Results.NotFound(new ApiError { Error = "project_not_found" });
            try
            {
                string full = project.WriteFile(body.Name, body.Content ?? string.Empty);
                long bytes = new FileInfo(full).Length;
                return Results.Ok(new WriteFileResponse { Name = body.Name, Path = full, Bytes = bytes });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ApiError { Error = "invalid_file_name", Detail = ex.Message });
            }
        });

        group.MapGet("/projects/{id}/files/{*name}", (string id, string name, SandboxProjectStore store) =>
        {
            var project = store.Get(id);
            if (project == null) return Results.NotFound(new ApiError { Error = "project_not_found" });
            var content = project.ReadFile(name);
            if (content == null) return Results.NotFound(new ApiError { Error = "file_not_found" });
            return Results.Text(content, "text/plain");
        });

        group.MapDelete("/projects/{id}", (string id, SandboxProjectStore store) =>
        {
            return store.Delete(id)
                ? Results.NoContent()
                : Results.NotFound(new ApiError { Error = "project_not_found" });
        });

        group.MapPost("/simulate", async (SimulateRequest req, HeadlessSimulator sim, ILoggerFactory lf) =>
        {
            var log = lf.CreateLogger("SimulationApi");
            try
            {
                var result = await sim.RunAsync(req);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new ApiError { Error = "bad_request", Detail = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.UnprocessableEntity(new ApiError { Error = "sim_unprocessable", Detail = ex.Message });
            }
            catch (Exception ex)
            {
                log.LogError(ex, "[SimulationApi] simulate failed");
                return Results.Problem(title: "sim_failed", detail: ex.Message, statusCode: 500);
            }
        });

        return app;
    }
}
#endif
