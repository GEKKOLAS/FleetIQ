FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/Services/Notifications/FleetIQ.Notifications.Api/FleetIQ.Notifications.Api.csproj
RUN dotnet publish src/Services/Notifications/FleetIQ.Notifications.Api/FleetIQ.Notifications.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "FleetIQ.Notifications.Api.dll"]
