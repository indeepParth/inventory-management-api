# ==========================
# Build Stage
# ==========================

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Copy solution
COPY InventoryManagement.sln ./

# Copy project files
COPY InventoryManagement.API/*.csproj InventoryManagement.API/
COPY InventoryManagement.Application/*.csproj InventoryManagement.Application/
COPY InventoryManagement.Domain/*.csproj InventoryManagement.Domain/
COPY InventoryManagement.Infrastructure/*.csproj InventoryManagement.Infrastructure/
COPY InventoryManagement.Tests/*.csproj InventoryManagement.Tests/

# Restore dependencies (Docker cache friendly)
RUN dotnet restore InventoryManagement.sln

# Copy source code
COPY . .

# Publish
RUN dotnet publish \
    InventoryManagement.API/InventoryManagement.API.csproj \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore

# ==========================
# Runtime Stage
# ==========================

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/Data /app/Logs

COPY --from=build /app/publish .

RUN chown -R $APP_UID:0 /app/Data /app/Logs

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl --fail --silent --show-error http://localhost:8080/health/ready > /dev/null || exit 1

USER $APP_UID

ENTRYPOINT ["dotnet", "InventoryManagement.API.dll"]
