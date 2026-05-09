# syntax=docker/dockerfile:1.7

ARG DOTNET_ASPNET_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0-noble
ARG PYTHON_VERSION=3.13.13
ARG PYTHON_SHA256=2ab91ff401783ccca64f75d10c882e957bdfd60e2bf5a72f8421793729b78a71
ARG PYTHON_PREFIX=/opt/python/3.13.13
ARG VENV_PATH=/opt/venv

# Ubuntu noble is used instead of Alpine because pythonnet, CoreCLR, and
# scientific Python packages are much better supported on glibc-based images.
# The ASP.NET runtime image is the correct parent for reusable ASP.NET Core app
# images because it includes the .NET runtime without SDK/build tooling.
FROM ${DOTNET_ASPNET_IMAGE} AS python-builder

ARG PYTHON_VERSION
ARG PYTHON_SHA256
ARG PYTHON_PREFIX
ARG VENV_PATH

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
    LD_LIBRARY_PATH="$PYTHON_PREFIX/lib${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}" "$PYTHON_PREFIX/bin/python3.13" --version

ENV VIRTUAL_ENV=${VENV_PATH}
ENV PATH="${VENV_PATH}/bin:${PYTHON_PREFIX}/bin:${PATH}"
ENV LD_LIBRARY_PATH="${PYTHON_PREFIX}/lib"

# /opt/venv keeps Python dependencies independent from the OS and from any
# downstream app. requirements.txt is copied before installation so package
# layers can be cached separately from application images that inherit this base.
COPY requirements.txt /tmp/requirements.txt

RUN set -eux; \
    python3.13 -m venv "$VIRTUAL_ENV"; \
    python -m pip install --no-cache-dir --upgrade pip setuptools wheel uv; \
    uv pip install --python "$VIRTUAL_ENV/bin/python" --no-cache -r /tmp/requirements.txt; \
    python -m pip uninstall -y uv; \
    python -m pip cache purge; \
    rm -rf /root/.cache /tmp/requirements.txt; \
    python -c "import numpy,pandas,pydantic,jellyfish,recordlinkage,pythonnet; print('package import OK')"

FROM ${DOTNET_ASPNET_IMAGE} AS runtime

ARG PYTHON_PREFIX
ARG VENV_PATH

ENV DEBIAN_FRONTEND=noninteractive

# Runtime libraries used by CPython and common scientific packages. Compilers
# and headers are intentionally excluded from the production image.
RUN set -eux; \
    apt-get update; \
    apt-get install -y --no-install-recommends \
        ca-certificates \
        libbz2-1.0 \
        libexpat1 \
        libffi8 \
        libgdbm6 \
        libgdbm-compat4 \
        libgomp1 \
        liblzma5 \
        libncursesw6 \
        libreadline8t64 \
        libsqlite3-0 \
        libssl3t64 \
        libstdc++6 \
        libuuid1 \
        tk \
        zlib1g; \
    rm -rf /var/lib/apt/lists/*

COPY --from=python-builder ${PYTHON_PREFIX} ${PYTHON_PREFIX}
COPY --from=python-builder ${VENV_PATH} ${VENV_PATH}

# Required defaults for reusable app images. Pythonnet needs CoreCLR settings
# because it hosts .NET from Python, while C# apps using Python.Runtime need the
# shared Python library path to initialize the embedded interpreter reliably.
ENV PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    VIRTUAL_ENV=${VENV_PATH} \
    PATH="${VENV_PATH}/bin:${PYTHON_PREFIX}/bin:${PATH}" \
    DOTNET_ROOT=/usr/share/dotnet \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    PYTHONNET_RUNTIME=coreclr \
    PYTHONNET_PYDLL=${PYTHON_PREFIX}/lib/libpython3.13.so \
    LD_LIBRARY_PATH=${PYTHON_PREFIX}/lib

# This base image is consumed with `FROM <this-image>` in downstream app
# Dockerfiles. It intentionally does not copy source code or run app build steps.
RUN set -eux; \
    python --version; \
    pip --version; \
    dotnet --info; \
    test -f "$PYTHONNET_PYDLL"; \
    python -c "import sys; print(sys.executable); print(sys.version)"; \
    python -c "import pythonnet; print('pythonnet import OK')"; \
    python -c "from pythonnet import load; load('coreclr'); import clr; print('coreclr load OK')"; \
    python -c "import numpy,pandas,pydantic,jellyfish,recordlinkage; print('scientific packages OK')"
