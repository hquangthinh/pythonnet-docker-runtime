# .NET 10 + Python 3.14 + pythonnet Base Image

Reusable production base image for ASP.NET Core applications that need Python 3.14, `pythonnet`, and Splink/DuckDB/Arrow data packages available in-process. This image contains the .NET ASP.NET runtime, CPython, and the Python packages listed in `requirements.txt`. It does not contain application source code or application build output.

## What Is Included

- Base image: `mcr.microsoft.com/dotnet/aspnet:10.0-noble`
- CPython: `3.14.5`, built with a shared Python library
- Python packages from `requirements.txt`, installed into `/opt/python/3.14.5/lib/python3.14/site-packages` (no virtual environment)
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
pythonnet==3.1.0
```

`pythonnet==3.1.0` is the first stable release that supports Python 3.14 (`pythonnet==3.0.5` declares `Requires-Python: <3.14`). The C# side must reference the NuGet package `pythonnet` version `3.1.0` or newer for the same reason.

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

To add more Python packages in a child image, install them straight into the interpreter:

```dockerfile
RUN pip install --no-cache-dir <package>
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
docker run --rm "$IMAGE" sh -c 'env -u PYTHONHOME -u PYTHONPATH /opt/python/3.14.5/bin/python3.14 -c "import splink, pythonnet; print(1)"'
docker run --rm -e LD_LIBRARY_PATH= "$IMAGE" python -c "import sys; print('rpath OK', sys.version)"
docker run --rm "$IMAGE" wkhtmltopdf --version
docker run --rm "$IMAGE" sh -c "printf '<html><body><h1>ok</h1></body></html>' > /tmp/in.html && wkhtmltopdf /tmp/in.html /tmp/out.pdf && test -s /tmp/out.pdf"
```

## Environment Variables

The final image sets:

```text
PYTHONUNBUFFERED=1
PYTHONDONTWRITEBYTECODE=1
PATH=/opt/python/3.14.5/bin:$PATH
PYTHONHOME=/opt/python/3.14.5
DOTNET_ROOT=/usr/share/dotnet
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
PYTHONNET_RUNTIME=coreclr
PYTHONNET_PYDLL=/opt/python/3.14.5/lib/libpython3.14.so
LD_LIBRARY_PATH=/opt/python/3.14.5/lib
```

`PYTHONNET_RUNTIME=coreclr` tells pythonnet to use the .NET Core runtime. `DOTNET_ROOT=/usr/share/dotnet` points pythonnet to the runtime included in the official ASP.NET image.

`PYTHONNET_PYDLL` points C# embedding scenarios to the shared Python library. This image builds Python with `--enable-shared`, so the expected path is:

```text
/opt/python/3.14.5/lib/libpython3.14.so
```

Packages are installed into the interpreter's own `site-packages`, not a virtual environment. When Python is embedded in a .NET process, `sys.executable` is `dotnet` and CPython would never activate a venv, but it always finds its own `site-packages`, so no extra configuration is needed. `PYTHONHOME` is set as a guard so the embedded interpreter always resolves this prefix.

`libpython3.14.so` and the `python3.14` binary are built with an rpath to `/opt/python/3.14.5/lib`, so they load even if a downstream image clears `LD_LIBRARY_PATH`. `LD_LIBRARY_PATH` is still set as a convenience for other native libraries.
