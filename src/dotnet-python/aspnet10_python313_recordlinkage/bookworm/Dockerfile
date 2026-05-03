FROM hquangthinh/dotnet-python:10.0-3.13-bookworm AS base

RUN pip install --upgrade pip setuptools wheel

RUN pip install --only-binary=:all: numpy==2.4.4
RUN pip install --only-binary=:all: pandas==2.3.3
RUN pip install --only-binary=:all: pydantic==2.13.3
RUN pip install --only-binary=:all: jellyfish==1.2.1
RUN pip install recordlinkage==0.16
RUN pip install pythonnet==3.0.5

ENV PYTHONNET_PYDLL /usr/local/lib/libpython3.13.so
ENV PYTHONNET_RUNTIME coreclr
ENV PYTHONPATH /app:$PYTHONPATH
