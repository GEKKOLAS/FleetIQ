FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/Services/Intelligence/FleetIQ.Intelligence.Api/FleetIQ.Intelligence.Api.csproj
RUN dotnet publish src/Services/Intelligence/FleetIQ.Intelligence.Api/FleetIQ.Intelligence.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "FleetIQ.Intelligence.Api.dll"]
