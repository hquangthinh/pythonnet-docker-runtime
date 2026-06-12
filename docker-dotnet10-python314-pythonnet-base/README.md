# .NET 10 + Python 3.14 + pythonnet Base Image

Reusable production base image for ASP.NET Core applications that need Python 3.14, `pythonnet`, and Splink/DuckDB/Arrow data packages available in-process. This image contains the .NET ASP.NET runtime, CPython, `/opt/venv`, and the Python packages listed in `requirements.txt`. It does not contain application source code or application build output.

## What Is Included

- Base image: `mcr.microsoft.com/dotnet/aspnet:10.0-noble`
- CPython: `3.14.5`, built with a shared Python library
- Virtual environment: `/opt/venv`
- Python packages from `requirements.txt`
- pythonnet configured for CoreCLR
- `wkhtmltopdf` from Ubuntu Noble packages

The image intentionally does not run `dotnet restore`, `dotnet publish`, `npm install`, or any other app build command.

## Build

```bash
docker build \
  -t local/dotnet10-python314-pythonnet-base:10.0-python3.14-noble \
  ./docker-dotnet10-python314-pythonnet-base
```

Tag for a registry:

```bash
docker tag \
  local/dotnet10-python314-pythonnet-base:10.0-python3.14-noble \
  <registry-or-user>/dotnet10-python314-pythonnet-base:10.0-python3.14-noble
```

Optional push:

```bash
docker push <registry-or-user>/dotnet10-python314-pythonnet-base:10.0-python3.14-noble
```

## Requirements

The `requirements.txt` is pinned for Python 3.14 compatibility:

```txt
splink==4.0.16
duckdb==1.5.3
jellyfish==1.2.1
polars==1.41.2
pyarrow==24.0.0
pydantic==2.13.4
pythonnet==3.1.0rc0
```

`pythonnet==3.1.0rc0` is used because `pythonnet==3.0.5` declares `Requires-Python: <3.14`.

## Use From An App Image

Build and publish your application in a separate Dockerfile. Use this image only as the runtime parent:

```dockerfile
FROM <registry-or-user>/dotnet10-python314-pythonnet-base:10.0-python3.14-noble AS runtime

WORKDIR /app

# Copy app output produced by a separate build stage, CI job, or SDK image.
# This base image intentionally does not build the app.
COPY ./publish/ ./

ENTRYPOINT ["dotnet", "YourApp.dll"]
```

## Verify The Image

```bash
IMAGE=local/dotnet10-python314-pythonnet-base:10.0-python3.14-noble

docker run --rm "$IMAGE" python --version
docker run --rm "$IMAGE" pip --version
docker run --rm "$IMAGE" dotnet --info
docker run --rm "$IMAGE" python -c "import sys; print(sys.executable); print(sys.version)"
docker run --rm "$IMAGE" python -c "import splink,duckdb,jellyfish,polars,pyarrow,pydantic,pythonnet; print('packages OK')"
docker run --rm "$IMAGE" python -c "from pythonnet import load; load('coreclr'); import clr; print('coreclr load OK')"
docker run --rm "$IMAGE" sh -c 'test -f "$PYTHONNET_PYDLL" && echo "$PYTHONNET_PYDLL"'
docker run --rm "$IMAGE" wkhtmltopdf --version
docker run --rm "$IMAGE" sh -c "printf '<html><body><h1>ok</h1></body></html>' > /tmp/in.html && wkhtmltopdf /tmp/in.html /tmp/out.pdf && test -s /tmp/out.pdf"
```

## Environment Variables

The final image sets:

```text
PYTHONUNBUFFERED=1
PYTHONDONTWRITEBYTECODE=1
VIRTUAL_ENV=/opt/venv
PATH=/opt/venv/bin:/opt/python/3.14.5/bin:$PATH
DOTNET_ROOT=/usr/share/dotnet
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
PYTHONNET_RUNTIME=coreclr
PYTHONNET_PYDLL=/opt/python/3.14.5/lib/libpython3.14.so
LD_LIBRARY_PATH=/opt/python/3.14.5/lib:$LD_LIBRARY_PATH
```

`PYTHONNET_RUNTIME=coreclr` tells pythonnet to use the .NET Core runtime. `DOTNET_ROOT=/usr/share/dotnet` points pythonnet to the runtime included in the official ASP.NET image.

`PYTHONNET_PYDLL` points C# embedding scenarios to the shared Python library. This image builds Python with `--enable-shared`, so the expected path is:

```text
/opt/python/3.14.5/lib/libpython3.14.so
```

`LD_LIBRARY_PATH` includes the same library directory so the dynamic linker can find `libpython3.14.so`.
