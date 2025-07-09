FROM hquangthinh/dotnet-python:10.0-3.12-bookworm AS base

RUN pip install --upgrade pip setuptools wheel
RUN pip install pythonnet==3.0.3

ENV PYTHONNET_PYDLL /usr/local/lib/libpython3.12.so
ENV PYTHONPATH /app:$PYTHONPATH
