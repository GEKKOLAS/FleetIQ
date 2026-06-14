FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/Services/Identity/FleetIQ.Identity.Api/FleetIQ.Identity.Api.csproj
RUN dotnet publish src/Services/Identity/FleetIQ.Identity.Api/FleetIQ.Identity.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "FleetIQ.Identity.Api.dll"]
