# Multi-stage build for the Orders API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/Services/Orders/FleetIQ.Orders.Api/FleetIQ.Orders.Api.csproj
RUN dotnet publish src/Services/Orders/FleetIQ.Orders.Api/FleetIQ.Orders.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "FleetIQ.Orders.Api.dll"]
