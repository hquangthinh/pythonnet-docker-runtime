FROM hquangthinh/dotnet-python:10.0-3.13-azurelinux AS base

RUN pip install --upgrade pip setuptools wheel

RUN pip install numpy==2.0.1
RUN pip install pandas==2.2.2
RUN pip install pydantic==2.8.2
RUN pip install jellyfish==1.1.0
RUN pip install recordlinkage==0.16
RUN pip install pythonnet==3.0.5

ENV PYTHONNET_PYDLL /usr/local/lib/libpython3.13.so
ENV PYTHONPATH /app:$PYTHONPATH
