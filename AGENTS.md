# AGENTS

## What This Repo Actually Does
- Builds Docker runtime images that combine ASP.NET runtime + CPython, with optional `pythonnet` and `recordlinkage` stacks.
- Source of truth is Dockerfiles in `src/dotnet-python/` and GitHub workflows in `.github/workflows/` (not the README table).

## Layout You Need To Understand
- Base images: `src/dotnet-python/<aspnetX_pythonY>/<distro>/Dockerfile`
- Derived images:
  - `..._pythonnet/<distro>/Dockerfile`
  - `..._recordlinkage/<distro>/Dockerfile`
- Standalone base images: `docker-dotnet10-python31{3,4}-pythonnet-base/Dockerfile`
- Example app consuming the 3.14 base: `examples/aspnet10-python314-splink-smoke/` (ASP.NET Core 10 + pythonnet + Scalar; workflow `.github/workflows/example-aspnet10-python314-splink-smoke.yml` builds, smoke-tests via curl, then pushes)
- Current distro matrix in tree:
  - `aspnet8/9/10 + python3.12` on `bookworm`
  - `aspnet10 + python3.13` on `bookworm`, `alpine3`, and `azurelinux`

## CI/CD Truths (Easy To Miss)
- `python3.12/bookworm` variants for `aspnet8/9/10` are wired in CI.
- `python3.13/bookworm` variants for `aspnet10` are also wired in CI (`.github/workflows/*python313*bookworm*`).
- `python3.13` on `alpine3` and `azurelinux` still have Dockerfiles but no workflow file in `.github/workflows/`.
- Base workflows trigger on `push`, `pull_request`, and `workflow_dispatch`, and use `docker/build-push-action` with `push: true`.
- Pythonnet/recordlinkage workflows run via `workflow_run` after base workflow completion.

## Tagging + Naming Conventions
- Workflow tags use `${DOCKER_USERNAME}/dotnet-python:<dotnet>-<python>-<distro>[-pythonnet|-pythonnet-recordlinkage]`.
- Do not trust README image names blindly: workflows are authoritative for pushed tag names.

## Local Build/Verify Commands
- Build a base image (example):
  - `docker build -t hquangthinh/dotnet-python:10.0-3.12-bookworm ./src/dotnet-python/aspnet10_python312/bookworm`
- Build derived image (example):
  - `docker build -t local/dotnet-python:10.0-3.12-bookworm-pythonnet ./src/dotnet-python/aspnet10_python312_pythonnet/bookworm`
- Important: derived Dockerfiles `FROM hquangthinh/dotnet-python:...`; local testing requires the base image to be tagged with that exact name/tag (or temporary local FROM edit not committed).
- Fast runtime checks:
  - `docker run --rm <image> python3 --version`
  - `docker run --rm <image> python -c "import clr"` (pythonnet images)
  - `docker run --rm <image> python -c "import numpy,pandas,pydantic,jellyfish,recordlinkage,clr"` (recordlinkage images)

## Dependency/Version Guardrails
- Base images compile CPython from source and verify with GPG; keep this flow intact unless intentionally changing supply-chain behavior.
- Derived images pin Python deps (`pythonnet`, `numpy`, `pandas`, `pydantic`, `jellyfish`, `recordlinkage`); change pins deliberately and keep variants aligned where expected.
- `PYTHONNET_PYDLL` must match Python major/minor in each derived image (`libpython3.12.so` vs `libpython3.13.so`).

## When Editing
- If you add a new image variant, update all three together:
  1. Dockerfile directory under `src/dotnet-python/`
  2. Matching workflow in `.github/workflows/` (if it should publish)
  3. `README.md`/`CHANGELOG.md` to reflect actual published tags/versions
