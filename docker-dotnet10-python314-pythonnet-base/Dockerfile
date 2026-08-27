# syntax=docker/dockerfile:1.7

ARG DOTNET_ASPNET_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0-noble
ARG PYTHON_VERSION=3.14.5
ARG PYTHON_SHA256=7e32597b99e5d9a39abed35de4693fa169df3e5850d4c334337ffd6a19a36db6
ARG PYTHON_PREFIX=/opt/python/${PYTHON_VERSION}

# Ubuntu noble is used instead of Alpine because pythonnet, CoreCLR, and
# scientific Python packages are much better supported on glibc-based images.
# The ASP.NET runtime image is the correct parent for reusable ASP.NET Core app
# images because it includes the .NET runtime without SDK/build tooling.
FROM ${DOTNET_ASPNET_IMAGE} AS python-builder

ARG PYTHON_VERSION
ARG PYTHON_SHA256
ARG PYTHON_PREFIX

ENV DEBIAN_FRONTEND=noninteractive

# Build dependencies stay in this builder stage. The final image receives only
# the compiled Python runtime, virtual environment, and runtime libraries.
RUN set -eux; \
    apt-get update; \
    apt-get install -y --no-install-recommends \
        build-essential \
        ca-certificates \
        curl \
        dpkg-dev \
        libbz2-dev \
        libexpat1-dev \
        libffi-dev \
        libgdbm-dev \
        liblzma-dev \
        libncursesw5-dev \
        libreadline-dev \
        libsqlite3-dev \
        libssl-dev \
        libzstd-dev \
        make \
        tk-dev \
        uuid-dev \
        wget \
        xz-utils \
        zlib1g-dev; \
    rm -rf /var/lib/apt/lists/*

RUN set -eux; \
    wget -O python.tar.xz "https://www.python.org/ftp/python/${PYTHON_VERSION}/Python-${PYTHON_VERSION}.tar.xz"; \
    echo "${PYTHON_SHA256} *python.tar.xz" | sha256sum -c -; \
    mkdir -p /usr/src/python; \
    tar --extract --directory /usr/src/python --strip-components=1 --file python.tar.xz; \
    rm python.tar.xz; \
    cd /usr/src/python; \
    gnuArch="$(dpkg-architecture --query DEB_BUILD_GNU_TYPE)"; \
    LDFLAGS="-Wl,-rpath,${PYTHON_PREFIX}/lib" \
    ./configure \
        --build="$gnuArch" \
        --prefix="$PYTHON_PREFIX" \
        --enable-loadable-sqlite-extensions \
        --enable-optimizations \
        --enable-shared \
        --with-lto \
        --with-system-expat \
        --with-ensurepip=install; \
    make -j "$(nproc)"; \
    make install; \
    cd /; \
    rm -rf /usr/src/python; \
    find "$PYTHON_PREFIX" -depth \
        \( \
            \( -type d -a \( -name test -o -name tests -o -name idle_test \) \) \
            -o \( -type f -a \( -name '*.pyc' -o -name '*.pyo' -o -name 'libpython*.a' \) \) \
        \) -exec rm -rf '{}' +; \
    ln -s python3.14 "$PYTHON_PREFIX/bin/python"; \
    ln -s pip3.14 "$PYTHON_PREFIX/bin/pip"; \
    "$PYTHON_PREFIX/bin/python" --version

ENV PATH="${PYTHON_PREFIX}/bin:${PATH}"
ENV LD_LIBRARY_PATH="${PYTHON_PREFIX}/lib"

# Packages are installed straight into the interpreter's own site-packages, not
# a virtual environment. The custom prefix already isolates this Python from the
# OS, and an embedded interpreter (C# hosting Python via Python.Runtime) only
# sees the prefix's site-packages, never a venv. requirements.txt is copied
# before installation so package layers cache separately from app images.
COPY requirements.txt /tmp/requirements.txt

RUN set -eux; \
    python -m pip install --no-cache-dir --upgrade pip setuptools wheel uv; \
    uv pip install --python "$PYTHON_PREFIX/bin/python3.14" --no-cache -r /tmp/requirements.txt; \
    python -m pip uninstall -y uv; \
    python -m pip cache purge; \
    rm -rf /root/.cache /tmp/requirements.txt; \
    python -c "import splink,duckdb,jellyfish,polars,pyarrow,pydantic,pythonnet; print('package import OK')"

FROM ${DOTNET_ASPNET_IMAGE} AS runtime

ARG PYTHON_PREFIX
ARG DEBIAN_FRONTEND=noninteractive

# Runtime libraries used by CPython and common scientific packages. Compilers
# and headers are intentionally excluded from the production image. wkhtmltopdf
# comes from Ubuntu Noble because the archived upstream packages target older
# distro libraries such as libjpeg.so.62 that Noble no longer ships.
RUN set -eux; \
    apt-get update; \
    apt-get install -y --no-install-recommends \
        ca-certificates \
        libbz2-1.0 \
        libexpat1 \
        libffi8 \
        libgdbm6t64 \
        libgdbm-compat4t64 \
        libgomp1 \
        liblzma5 \
        libncursesw6 \
        libreadline8t64 \
        libsqlite3-0 \
        libssl3t64 \
        libstdc++6 \
        libuuid1 \
        libzstd1 \
        tk \
        wkhtmltopdf \
        zlib1g; \
    rm -rf /var/lib/apt/lists/*

COPY --from=python-builder ${PYTHON_PREFIX} ${PYTHON_PREFIX}

# Required defaults for reusable app images. Pythonnet needs CoreCLR settings
# because it hosts .NET from Python, while C# apps using Python.Runtime need the
# shared Python library path to initialize the embedded interpreter reliably.
#
# All packages live in the prefix's own site-packages, so the `python` binary
# and an embedded interpreter hosted by a .NET process see the same set with no
# extra configuration. PYTHONHOME is set as a guard so embedding always resolves
# this prefix. libpython is built with an rpath to its own lib dir, so
# LD_LIBRARY_PATH is a convenience for third-party .so files, not a requirement.
ENV PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    PATH="${PYTHON_PREFIX}/bin:${PATH}" \
    PYTHONHOME=${PYTHON_PREFIX} \
    DOTNET_ROOT=/usr/share/dotnet \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    PYTHONNET_RUNTIME=coreclr \
    PYTHONNET_PYDLL=${PYTHON_PREFIX}/lib/libpython3.14.so \
    LD_LIBRARY_PATH=${PYTHON_PREFIX}/lib

# This base image is consumed with `FROM <this-image>` in downstream app
# Dockerfiles. It intentionally does not copy source code or run app build steps.
RUN set -eux; \
    python --version; \
    pip --version; \
    dotnet --info; \
    test -f "$PYTHONNET_PYDLL"; \
    if ldd "$PYTHONNET_PYDLL" | grep -q 'not found'; then ldd "$PYTHONNET_PYDLL"; exit 1; fi; \
    python -c "import sys; print(sys.executable); print(sys.version)"; \
    env -u LD_LIBRARY_PATH python -c "import sys; print('rpath OK', sys.version)"; \
    env -u PYTHONHOME -u PYTHONPATH "$PYTHON_PREFIX/bin/python3.14" -c "import splink, pythonnet; print('embedded-style import OK')"; \
    python -c "import pythonnet; print('pythonnet import OK')"; \
    python -c "from pythonnet import load; load('coreclr'); import clr; print('coreclr load OK')"; \
    python -c "import splink,duckdb,jellyfish,polars,pyarrow,pydantic; print('data packages OK')"; \
    wkhtmltopdf --version
