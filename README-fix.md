# Fix: .NET 10 OpenAPI + migrations

## What changed and why
- **Removed Swashbuckle 7.2.0** - it is not compatible with .NET 10 (the `GetSwagger` crash you saw,
  caused by Microsoft.OpenApi 2.0). Replaced it with the **native .NET 10 OpenAPI** (`AddOpenApi`/`MapOpenApi`),
  which is what Microsoft now ships by default. The OpenAPI document is at **/openapi/v1.json**.
- **Added a design-time `IDesignTimeDbContextFactory` per service.** `dotnet ef` now creates the DbContext
  directly (with a dev connection string) instead of building the whole web host - this fixes the
  "Unable to resolve DbContextOptions" / host-crash errors during `add-migrations`.

## Re-run
```powershell
# from the repo root (fleetiq\)
.\scriptsdd-migrations.ps1     # should now succeed for all 12
.\scriptsun-all.ps1
```

## Testing endpoints
Use the **Postman collection** (handles the token + bearer auth for every service).
The OpenAPI JSON is also available per service at `/openapi/v1.json`.

## Optional: browser UI (Scalar - the .NET 10 Swagger replacement)
If you want an in-browser UI with an auth field, add to `BuildingBlocks.Web`:
```
<PackageReference Include="Scalar.AspNetCore" />   <!-- + a PackageVersion in Directory.Packages.props -->
```
then in `OpenApiExtensions.MapFleetIqSwagger`, after `app.MapOpenApi();` add:
```csharp
app.MapScalarApiReference();   // using Scalar.AspNetCore;  -> UI at /scalar
```
Left out of this build on purpose so a package-version mismatch can't block you again.
