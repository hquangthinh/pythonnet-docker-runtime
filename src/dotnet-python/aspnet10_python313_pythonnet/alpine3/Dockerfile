FROM hquangthinh/dotnet-python:10.0-3.13-alpine AS base

RUN pip install --upgrade pip setuptools wheel
RUN pip install pythonnet==3.0.5

ENV PYTHONNET_PYDLL /usr/local/lib/libpython3.13.so
ENV PYTHONPATH /app:$PYTHONPATH
