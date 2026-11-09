FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/Services/Inventory/FleetIQ.Inventory.Api/FleetIQ.Inventory.Api.csproj
RUN dotnet publish src/Services/Inventory/FleetIQ.Inventory.Api/FleetIQ.Inventory.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "FleetIQ.Inventory.Api.dll"]
