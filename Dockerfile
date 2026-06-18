# Build Stage

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY . .

RUN dotnet restore InventoryManagement.sln

RUN dotnet publish \
    InventoryManagement.API/InventoryManagement.API.csproj \
    -c Release \
    -o /app/publish

# Runtime Stage

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "InventoryManagement.API.dll"]