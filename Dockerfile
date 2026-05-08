# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY noo.api.sln ./
COPY src/Noo.Api/Noo.Api.csproj src/Noo.Api/
COPY tests/Noo.IntegrationTests/Noo.IntegrationTests.csproj tests/Noo.IntegrationTests/
COPY tests/Noo.UnitTests/Noo.UnitTests.csproj tests/Noo.UnitTests/
RUN dotnet restore src/Noo.Api/Noo.Api.csproj

COPY src/ src/
RUN dotnet publish src/Noo.Api/Noo.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

RUN apk add --no-cache icu-libs icu-data-full tzdata

COPY --from=build /app/publish ./
RUN mkdir -p /app/var && chown -R app:app /app
USER app

ENV ASPNETCORE_URLS=http://+:5001 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 5001
ENTRYPOINT ["dotnet", "Noo.Api.dll"]
