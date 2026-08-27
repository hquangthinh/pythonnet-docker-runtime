using Microsoft.AspNetCore.Http.HttpResults;
using SplinkSmoke.Api.Python;

namespace SplinkSmoke.Api.Endpoints;

public static class SmokeTestEndpoints
{
    public static IEndpointRouteBuilder MapSmokeTestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/python").WithTags("Python smoke tests");

        group.MapGet("/info", (PythonSmokeTestRunner runner) => runner.GetInfo())
            .WithName("GetPythonInfo")
            .WithSummary("Embedded Python interpreter details")
            .WithDescription("Version, executable, prefix and shared-library path of the Python engine hosted in-process via pythonnet.");

        group.MapGet("/tests", (PythonSmokeTestRunner runner) => runner.ListTests())
            .WithName("ListSmokeTests")
            .WithSummary("List available package smoke tests");

        group.MapPost("/tests/run", Results<Ok<SmokeTestRunResponse>, JsonHttpResult<SmokeTestRunResponse>> (PythonSmokeTestRunner runner) =>
            {
                var results = runner.RunAll();
                var response = new SmokeTestRunResponse(results.All(r => r.Ok), results);
                return response.AllPassed
                    ? TypedResults.Ok(response)
                    : TypedResults.Json(response, statusCode: StatusCodes.Status500InternalServerError);
            })
            .WithName("RunAllSmokeTests")
            .WithSummary("Run every package smoke test")
            .WithDescription("Exercises splink, duckdb, jellyfish, polars, pyarrow and pydantic inside the embedded interpreter. Returns 200 when all pass, 500 when any fail.");

        group.MapPost("/tests/run/{name}", Results<Ok<SmokeTestResult>, JsonHttpResult<SmokeTestResult>, NotFound<string>> (string name, PythonSmokeTestRunner runner) =>
            {
                var result = runner.RunOne(name);
                if (result is null)
                {
                    return TypedResults.NotFound($"Unknown test '{name}'. See GET /api/python/tests.");
                }
                return result.Ok
                    ? TypedResults.Ok(result)
                    : TypedResults.Json(result, statusCode: StatusCodes.Status500InternalServerError);
            })
            .WithName("RunSmokeTest")
            .WithSummary("Run one package smoke test by name");

        app.MapGet("/health", (PythonEngineHost engine) =>
                engine.IsReady
                    ? Results.Ok(new { status = "healthy", python = true })
                    : Results.Json(new { status = "unhealthy", python = false }, statusCode: StatusCodes.Status503ServiceUnavailable))
            .WithTags("Health")
            .WithName("Health")
            .WithSummary("Liveness check; 503 until the Python engine is initialised");

        return app;
    }
}
