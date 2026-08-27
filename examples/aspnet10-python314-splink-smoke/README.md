# Example: ASP.NET Core 10 + embedded Python 3.14 (Splink smoke tests)

A small ASP.NET Core 10 app that proves the `dotnet10-python314-pythonnet-base` image works as a runtime parent for a real .NET app that hosts Python in-process.

What it does:

- Uses `hquangthinh/dotnet10-python314-pythonnet-base:10.0-python3.14-noble` as the runtime parent image.
- Starts the embedded CPython interpreter with `pythonnet` (`Python.Runtime`) when the app starts, and shuts it down when the app stops.
- Ships a custom Python module (`python_modules/splink_smoke`) that smoke-tests `splink`, `duckdb`, `jellyfish`, `polars`, `pyarrow` and `pydantic`.
- Exposes a REST API to run those tests, with a Scalar UI at `/scalar/v1`.
- Is built, smoke-tested and pushed to Docker Hub by `.github/workflows/example-aspnet10-python314-splink-smoke.yml`.

Published image: `hquangthinh/dotnet10-python314-splink-smoke-example:10.0-python3.14-noble` (also `:latest`).

## Layout

```text
Dockerfile                          multi-stage: SDK build -> base image runtime
SplinkSmoke.Api/
  Program.cs                        DI, OpenAPI, Scalar, endpoints
  Python/PythonEngineHost.cs        IHostedService: PythonEngine.Initialize/Shutdown
  Python/PythonSmokeTestRunner.cs   Calls the Python module under Py.GIL()
  Python/SmokeTestModels.cs         Response records
  Endpoints/SmokeTestEndpoints.cs   Minimal API routes
  python_modules/splink_smoke/      Custom Python package copied to the app output
```

## API

| Method | Route                          | Purpose                                              |
|--------|--------------------------------|------------------------------------------------------|
| GET    | `/`                            | Redirects to the Scalar UI                           |
| GET    | `/scalar/v1`                   | Scalar API reference UI                              |
| GET    | `/openapi/v1.json`             | OpenAPI document                                     |
| GET    | `/health`                      | 200 when the Python engine is ready, else 503        |
| GET    | `/api/python/info`             | Python version, executable, prefix, shared library   |
| GET    | `/api/python/tests`            | Names of the available smoke tests                   |
| POST   | `/api/python/tests/run`        | Run all tests. 200 if all pass, 500 if any fail      |
| POST   | `/api/python/tests/run/{name}` | Run one test. 404 for an unknown name                |

Each test result looks like:

```json
{ "name": "splink", "package": "splink", "version": "4.0.16", "ok": true, "detail": "dedupe of 5 records produced 2 candidate pairs" }
```

## Build and run

```bash
# From the repository root. Pulls the published base image from Docker Hub.
docker build -t local/splink-smoke ./examples/aspnet10-python314-splink-smoke

# Or build against a locally built base image:
docker build \
  --build-arg BASE_IMAGE=local/dotnet10-python314-pythonnet-base:10.0-python3.14-noble \
  -t local/splink-smoke ./examples/aspnet10-python314-splink-smoke

docker run --rm -p 8080:8080 local/splink-smoke
```

Then open <http://localhost:8080/scalar/v1> or use curl:

```bash
curl http://localhost:8080/health
curl http://localhost:8080/api/python/info
curl http://localhost:8080/api/python/tests
curl -X POST http://localhost:8080/api/python/tests/run
curl -X POST http://localhost:8080/api/python/tests/run/splink
```

## How the Python engine is hosted

`PythonEngineHost` runs as an `IHostedService`:

1. Reads `PYTHONNET_PYDLL` (set by the base image to `/opt/python/3.14.5/lib/libpython3.14.so`). For local development outside Docker, set the env var or `PythonSmoke:PythonDll` in `appsettings.json`.
2. Sets `Runtime.PythonDLL` and calls `PythonEngine.Initialize()`.
3. Appends `<app>/python_modules` to `sys.path` and imports `splink_smoke.smoke_tests` once, so a broken module fails the app at startup.
4. Calls `PythonEngine.BeginAllowThreads()` so request threads can take the GIL.

`PythonSmokeTestRunner` wraps every call in `using (Py.GIL())` and serialises calls with a semaphore. Python exceptions inside a test are caught in Python and reported as `ok=false`; they never cross the pythonnet boundary.

## Running the Python module on its own

```bash
docker run --rm \
  -v "$PWD/examples/aspnet10-python314-splink-smoke/SplinkSmoke.Api/python_modules:/mod:ro" \
  -e PYTHONPATH=/mod \
  hquangthinh/dotnet10-python314-pythonnet-base:10.0-python3.14-noble \
  python -m splink_smoke.smoke_tests
```

## NuGet packages

- `pythonnet` 3.1.0 (first stable release supporting Python 3.14)
- `Microsoft.AspNetCore.OpenApi` 10.0.11
- `Scalar.AspNetCore` 2.17.1
