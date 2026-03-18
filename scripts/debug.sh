#!/usr/bin/env sh

# Set environment variables for debugging
export ASPNETCORE_ENVIRONMENT=Development

# Run the application
dotnet run --project ./src/Noo.Api/Noo.Api.csproj
