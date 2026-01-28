# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln .
COPY global.json .
COPY Directory.Build.props .
COPY Directory.Packages.props .
COPY src/Inventory.Domain/*.csproj src/Inventory.Domain/
COPY src/Inventory.Application/*.csproj src/Inventory.Application/
COPY src/Inventory.Infrastructure/*.csproj src/Inventory.Infrastructure/
COPY src/Inventory.Api/*.csproj src/Inventory.Api/
COPY tests/Inventory.Tests/*.csproj tests/Inventory.Tests/

# Restore packages
RUN dotnet restore src/Inventory.Api/Inventory.Api.csproj -p:RestoreFallbackFolders=


# Copy source code
COPY src/ src/
COPY tests/ tests/

# Build and publish
RUN dotnet publish src/Inventory.Api/Inventory.Api.csproj -c Release -o /app/publish --no-restore -p:RestoreFallbackFolders=

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

# Create non-root user
USER app

# Copy published app
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Set environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

ENTRYPOINT ["dotnet", "Inventory.Api.dll"]
