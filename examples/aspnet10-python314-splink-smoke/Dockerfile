# syntax=docker/dockerfile:1.7

# Runtime parent: the standalone .NET 10 + Python 3.14 + pythonnet base image.
# Override for local builds, e.g. --build-arg BASE_IMAGE=local/dotnet10-python314-pythonnet-base:test
ARG BASE_IMAGE=hquangthinh/dotnet10-python314-pythonnet-base:10.0-python3.14-noble

FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src

# Restore first so NuGet layers cache independently of source changes.
COPY SplinkSmoke.Api/SplinkSmoke.Api.csproj SplinkSmoke.Api/
RUN dotnet restore SplinkSmoke.Api/SplinkSmoke.Api.csproj

COPY SplinkSmoke.Api/ SplinkSmoke.Api/
RUN dotnet publish SplinkSmoke.Api/SplinkSmoke.Api.csproj -c Release -o /app/publish --no-restore

FROM ${BASE_IMAGE} AS runtime
WORKDIR /app

# The base image already exports PYTHONHOME, PYTHONNET_PYDLL and LD_LIBRARY_PATH,
# so the app needs no Python-specific configuration here.
COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SplinkSmoke.Api.dll"]
