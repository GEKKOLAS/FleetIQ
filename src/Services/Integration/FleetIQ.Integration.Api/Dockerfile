FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/Services/Integration/FleetIQ.Integration.Api/FleetIQ.Integration.Api.csproj
RUN dotnet publish src/Services/Integration/FleetIQ.Integration.Api/FleetIQ.Integration.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "FleetIQ.Integration.Api.dll"]
