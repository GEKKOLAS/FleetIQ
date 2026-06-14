FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/Gateway/FleetIQ.Gateway/FleetIQ.Gateway.csproj
RUN dotnet publish src/Gateway/FleetIQ.Gateway/FleetIQ.Gateway.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "FleetIQ.Gateway.dll"]
