# .NET 10 + Python 3.13 + pythonnet Base Image

Reusable production base image for ASP.NET Core applications that need Python 3.13 and `pythonnet` available in-process. This image contains the .NET ASP.NET runtime, CPython, `/opt/venv`, and the Python packages listed in `requirements.txt`. It does not contain application source code or application build output.

## What Is Included

- Base image: `mcr.microsoft.com/dotnet/aspnet:10.0-noble`
- CPython: `3.13.13`, built with a shared Python library
- Virtual environment: `/opt/venv`
- Python packages from `requirements.txt`
- pythonnet configured for CoreCLR

The image intentionally does not run `dotnet restore`, `dotnet publish`, `npm install`, or any other app build command.

## Build

```bash
docker build \
  -t local/dotnet10-python313-pythonnet-base:10.0-python3.13-noble \
  ./docker-dotnet10-python313-pythonnet-base
```

Tag for a registry:

```bash
docker tag \
  local/dotnet10-python313-pythonnet-base:10.0-python3.13-noble \
  <registry-or-user>/dotnet10-python313-pythonnet-base:10.0-python3.13-noble
```

Optional push:

```bash
docker push <registry-or-user>/dotnet10-python313-pythonnet-base:10.0-python3.13-noble
```

## Requirements

The sample `requirements.txt` is pinned for Python 3.13 compatibility:

```txt
numpy==2.4.4
pandas==2.3.3
pydantic==2.13.3
jellyfish==1.2.1
recordlinkage==0.16
pythonnet==3.0.5
```

`recordlinkage==0.16` is currently the latest `recordlinkage` release. `pythonnet==3.0.5` is used because older `pythonnet==3.0.3` declares `Requires-Python: <3.13`.

## Use From An App Image

Build and publish your application in a separate Dockerfile. Use this image only as the runtime parent:

```dockerfile
FROM <registry-or-user>/dotnet10-python313-pythonnet-base:10.0-python3.13-noble AS runtime

WORKDIR /app

# Copy app output produced by a separate build stage, CI job, or SDK image.
# This base image intentionally does not build the app.
COPY ./publish/ ./

ENTRYPOINT ["dotnet", "YourApp.dll"]
```

For multi-stage application builds, keep SDK/build tooling in the app Dockerfile's build stage, then copy published output into this runtime base.

## Verify The Image

```bash
IMAGE=local/dotnet10-python313-pythonnet-base:10.0-python3.13-noble

docker run --rm "$IMAGE" python --version
docker run --rm "$IMAGE" pip --version
docker run --rm "$IMAGE" dotnet --info
docker run --rm "$IMAGE" python -c "import sys; print(sys.executable); print(sys.version)"
docker run --rm "$IMAGE" python -c "import numpy,pandas,pydantic,jellyfish,recordlinkage,pythonnet; print('packages OK')"
docker run --rm "$IMAGE" python -c "from pythonnet import load; load('coreclr'); import clr; print('coreclr load OK')"
docker run --rm "$IMAGE" sh -c 'test -f "$PYTHONNET_PYDLL" && echo "$PYTHONNET_PYDLL"'
```

## Environment Variables

The final image sets:

```text
PYTHONUNBUFFERED=1
PYTHONDONTWRITEBYTECODE=1
VIRTUAL_ENV=/opt/venv
PATH=/opt/venv/bin:/opt/python/3.13.13/bin:$PATH
DOTNET_ROOT=/usr/share/dotnet
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
PYTHONNET_RUNTIME=coreclr
PYTHONNET_PYDLL=/opt/python/3.13.13/lib/libpython3.13.so
LD_LIBRARY_PATH=/opt/python/3.13.13/lib:$LD_LIBRARY_PATH
```

`PYTHONNET_RUNTIME=coreclr` tells pythonnet to use the .NET Core runtime. `DOTNET_ROOT=/usr/share/dotnet` points pythonnet to the runtime included in the official ASP.NET image.

`PYTHONNET_PYDLL` points C# embedding scenarios to the shared Python library. This image builds Python with `--enable-shared`, so the expected path is:

```text
/opt/python/3.13.13/lib/libpython3.13.so
```

`LD_LIBRARY_PATH` includes the same library directory so the dynamic linker can find `libpython3.13.so`.

## Find The Python Shared Library

Inside the image:

```bash
python - <<'PY'
import pathlib
for path in pathlib.Path("/opt/python").rglob("libpython3.13.so*"):
    print(path)
PY
```

You usually only need to override `PYTHONNET_PYDLL` if you replace Python, move the installation prefix, or consume this venv from a different runtime image.

Set or extend `LD_LIBRARY_PATH` if `ldd "$PYTHONNET_PYDLL"` reports unresolved libraries or if a downstream image relocates Python libraries.

## Test pythonnet From Python

```bash
python -c "import pythonnet; print('pythonnet import OK')"
python -c "from pythonnet import load; load('coreclr'); import clr; print('coreclr load OK')"
```

If CoreCLR loading fails, check:

```bash
echo "$DOTNET_ROOT"
dotnet --info
echo "$PYTHONNET_RUNTIME"
echo "$LD_LIBRARY_PATH"
```

## Test Python.Runtime From C#

In your application, reference `Python.Runtime` and initialize Python using the image defaults:

```csharp
using Python.Runtime;

var pythonDll = Environment.GetEnvironmentVariable("PYTHONNET_PYDLL");
if (!string.IsNullOrWhiteSpace(pythonDll))
{
    Runtime.PythonDLL = pythonDll;
}

PythonEngine.Initialize();
using (Py.GIL())
{
    dynamic sys = Py.Import("sys");
    Console.WriteLine(sys.version);
}
PythonEngine.Shutdown();
```

If initialization fails from C#, verify that the app process inherits `PYTHONNET_PYDLL`, `VIRTUAL_ENV`, `PATH`, and `LD_LIBRARY_PATH`.

## Troubleshooting Package Builds

- Prefer wheels for scientific packages on Python 3.13. Old pins may not publish `cp313` wheels and can force slow or failing source builds.
- If a package needs native compilation, add the build dependency only in the Dockerfile builder stage, not in the final stage.
- If a package imports successfully during `docker build` but fails at runtime, the final stage is likely missing a shared runtime library. Run `ldd` against the failing extension module and add only the needed runtime package.
- Keep `requirements.txt` copied before install steps to preserve Docker layer caching.

## Troubleshooting pythonnet

- Confirm the .NET runtime exists with `dotnet --info`.
- Confirm CoreCLR mode with `echo "$PYTHONNET_RUNTIME"`.
- Confirm Python shared library path with `test -f "$PYTHONNET_PYDLL"`.
- Confirm the linker can find Python with `ldd "$PYTHONNET_PYDLL"`.
- Use `pythonnet==3.0.5` or newer compatible releases for Python 3.13. `pythonnet==3.0.3` is for Python versions earlier than 3.13.
