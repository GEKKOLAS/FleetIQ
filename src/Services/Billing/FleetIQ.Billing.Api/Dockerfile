FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/Services/Billing/FleetIQ.Billing.Api/FleetIQ.Billing.Api.csproj
RUN dotnet publish src/Services/Billing/FleetIQ.Billing.Api/FleetIQ.Billing.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "FleetIQ.Billing.Api.dll"]
