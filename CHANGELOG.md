# Changelog

## [Unreleased]

### Added

- Add standalone `docker-dotnet10-python314-pythonnet-base` image with CPython 3.14.5, pythonnet, Splink, DuckDB, Polars, PyArrow, Jellyfish, and Pydantic pins.
- Add GitHub workflow to build and publish `dotnet10-python314-pythonnet-base:10.0-python3.14-noble`.
- Add `aspnet10 + python3.13 + bookworm` Dockerfiles for base, pythonnet, and pythonnet-recordlinkage images.
- Add GitHub workflows to build and publish `10.0-3.13-bookworm` base, pythonnet, and pythonnet-recordlinkage tags.

### Changed

- Add Ubuntu Noble `wkhtmltopdf` runtime support to the standalone Python 3.14 pythonnet base image.
- Configure 3.13 pythonnet-based images to use CoreCLR by default (`PYTHONNET_RUNTIME=coreclr`) so `import clr` works on ASP.NET images.
- Update 3.13 bookworm recordlinkage dependency pins to wheel-compatible versions (`numpy/pandas/pydantic/jellyfish`) for reliable builds.

- Update README image/tag documentation to match workflow-published naming and current CI coverage.
- Add AGENTS.md with repository-specific guidance for image layout, CI behavior, and local verification commands.

## [0.0.3] - 2025-07-09

### Added

- Rebuild the base images with sdk 10.0.302
- 
## [0.0.2] - 2025-01-21

### Added

- Rebuild the base images with sdk 9.0.302


## [0.0.1] - 2024-11-18

### Added

- Rebuild the base images with sdk 8.0.11
