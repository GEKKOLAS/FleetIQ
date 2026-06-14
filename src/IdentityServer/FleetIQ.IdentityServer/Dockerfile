FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/IdentityServer/FleetIQ.IdentityServer/FleetIQ.IdentityServer.csproj
RUN dotnet publish src/IdentityServer/FleetIQ.IdentityServer/FleetIQ.IdentityServer.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "FleetIQ.IdentityServer.dll"]
