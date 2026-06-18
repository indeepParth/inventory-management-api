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

RUN dotnet build \
    InventoryManagement.API/InventoryManagement.API.csproj \
    -c $BUILD_CONFIGURATION \
    --no-restore

RUN dotnet publish \
    InventoryManagement.API/InventoryManagement.API.csproj \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-build

# ==========================
# Runtime Stage
# ==========================

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "InventoryManagement.API.dll"]