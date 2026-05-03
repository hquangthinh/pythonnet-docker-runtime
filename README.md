# pythonnet-docker-runtime

Docker image definitions that combine ASP.NET runtime + CPython, with optional `pythonnet` and `recordlinkage` stacks.

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

## Dependency stacks

- `pythonnet` images: `pythonnet==3.0.3` (3.12) or `pythonnet==3.0.5` (3.13)
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
