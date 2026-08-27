# pythonnet-docker-runtime

Docker image definitions that combine ASP.NET runtime + CPython, with optional `pythonnet` and `recordlinkage` stacks.

The repository also contains standalone ASP.NET 10 + CPython base images under `docker-dotnet10-*-pythonnet-base/` for application Dockerfiles that want a richer prebuilt runtime parent.

## Published image tags (CI-managed)

GitHub Actions currently publishes these variants to:

`<docker-username>/dotnet-python:<dotnet>-<python>-<distro>[-pythonnet|-pythonnet-recordlinkage]`

| Dotnet | Python | Distro   | Base tag                                            | Pythonnet tag                                                      | Pythonnet + recordlinkage tag                                                                            |
|--------|--------|----------|-----------------------------------------------------|--------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------|
| 8.0    | 3.12   | bookworm | `<user>/dotnet-python:8.0-3.12-bookworm`           | `<user>/dotnet-python:8.0-3.12-bookworm-pythonnet`                | `<user>/dotnet-python:8.0-3.12-bookworm-pythonnet-recordlinkage`                                        |
| 9.0    | 3.12   | bookworm | `<user>/dotnet-python:9.0-3.12-bookworm`           | `<user>/dotnet-python:9.0-3.12-bookworm-pythonnet`                | `<user>/dotnet-python:9.0-3.12-bookworm-pythonnet-recordlinkage`                                        |
| 10.0   | 3.12   | bookworm | `<user>/dotnet-python:10.0-3.12-bookworm`          | `<user>/dotnet-python:10.0-3.12-bookworm-pythonnet`               | `<user>/dotnet-python:10.0-3.12-bookworm-pythonnet-recordlinkage`                                       |
| 10.0   | 3.13   | bookworm | `<user>/dotnet-python:10.0-3.13-bookworm`          | `<user>/dotnet-python:10.0-3.13-bookworm-pythonnet`               | `<user>/dotnet-python:10.0-3.13-bookworm-pythonnet-recordlinkage`                                       |

## Variants present in source (not yet published by workflows)

- `aspnet10 + python3.13 + alpine3` (base/pythonnet/recordlinkage)
- `aspnet10 + python3.13 + azurelinux` (base/pythonnet/recordlinkage)

## Standalone pythonnet base images

These workflows publish separate Docker Hub repositories instead of the `dotnet-python:<tag>` matrix:

| Image repository | Tag |
|------------------|-----|
| `<user>/dotnet10-python313-pythonnet-base` | `10.0-python3.13-noble` |
| `<user>/dotnet10-python314-pythonnet-base` | `10.0-python3.14-noble` |

## Example app

`examples/aspnet10-python314-splink-smoke/` is an ASP.NET Core 10 app that uses `dotnet10-python314-pythonnet-base` as its runtime parent. It starts the embedded Python engine with pythonnet at app startup, runs smoke tests for `splink`, `duckdb`, `jellyfish`, `polars`, `pyarrow` and `pydantic` from a custom Python module, and exposes them through a REST API with a Scalar UI at `/scalar/v1`.

| Image repository | Tag |
|------------------|-----|
| `<user>/dotnet10-python314-splink-smoke-example` | `10.0-python3.14-noble` |

```bash
docker run --rm -p 8080:8080 hquangthinh/dotnet10-python314-splink-smoke-example:10.0-python3.14-noble
curl -X POST http://localhost:8080/api/python/tests/run
```

See `examples/aspnet10-python314-splink-smoke/README.md` for details.

## Dependency stacks

- `pythonnet` images: `pythonnet==3.0.3` (3.12) or `pythonnet==3.0.5` (3.13)
- Standalone `dotnet10-python314-pythonnet-base` uses `pythonnet==3.1.0rc0` because `pythonnet==3.0.5` caps support at `<3.14`, and adds:
  - `splink==4.0.16`
  - `duckdb==1.5.3`
  - `jellyfish==1.2.1`
  - `polars==1.41.2`
  - `pyarrow==24.0.0`
  - `pydantic==2.13.4`
- `recordlinkage` images (3.12/bookworm) add:
  - `numpy==2.0.1`
  - `pandas==2.2.2`
  - `pydantic==2.8.2`
  - `jellyfish==1.1.0`
  - `recordlinkage==0.16`
- `recordlinkage` image (10.0-3.13-bookworm) adds:
  - `numpy==2.4.4`
  - `pandas==2.3.3`
  - `pydantic==2.13.3`
  - `jellyfish==1.2.1`
  - `recordlinkage==0.16`

## Build locally

Build a base image:

```bash
docker build -t hquangthinh/dotnet-python:10.0-3.12-bookworm ./src/dotnet-python/aspnet10_python312/bookworm
```

Build a derived image:

```bash
docker build -t local/dotnet-python:10.0-3.12-bookworm-pythonnet ./src/dotnet-python/aspnet10_python312_pythonnet/bookworm
```

Note: derived Dockerfiles use `FROM hquangthinh/dotnet-python:...`, so for local builds you should build/tag the matching base image first.

Quick runtime checks:

```bash
docker run --rm <image> python3 --version
docker run --rm <image> python -c "import clr"
docker run --rm <image> python -c "import numpy,pandas,pydantic,jellyfish,recordlinkage,clr"
```
