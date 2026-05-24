# Inventory Physical Verification & Reconciliation

Enterprise Blazor Web App starter for IT asset inventory physical verification, variance reconciliation, approvals, audit history, and executive reporting.

## What Is Included

- `.NET 9` Clean Architecture solution: Domain, Application, Infrastructure, Web, Tests.
- Blazor Web App UI with MudBlazor and Fluent UI services.
- Azure Entra ID ready authentication with development fallback identity.
- EF Core SQL Server model for asset master, snapshots, verification, immutable audit logs, attachments, notifications, workflow approvals, and role assignments.
- Excel import preview/profiling pipeline using dynamic column mapping and duplicate detection.
- Reconciliation engine with variance classification and confidence scoring.
- Minimal APIs for assets, import preview, and reconciliation preview.
- Dashboard, upload, explorer, verification, reconciliation, audit, analytics, reports, approvals, notifications, roles, admin, and mobile pages.
- Docker, GitHub Actions, Azure deployment notes, and SQL schema.

## Workbook Profile Used

The supplied workbook `%USERPROFILE%\Downloads\IT Asset inv.xlsx` contains:

- 1 sheet, `Sheet1`
- 1,052 asset rows
- 37 columns
- 1 duplicate asset tag: `QDB0631`
- 1 duplicate serial number: `HPH5VX3`
- Several formula-error source columns, including `EMP NAME`, `Department`, `E-mail ID`, `Age (Y)`, and `Life span`

The import module is designed to surface these issues before committing a versioned inventory snapshot.

## Project Structure

```text
src/
  InventoryReconciliation.Domain/          Entities, value objects, enums
  InventoryReconciliation.Application/     DTOs, abstractions, validators, reconciliation engine
  InventoryReconciliation.Infrastructure/  EF Core, Excel reader, Azure Blob storage, current user service
  InventoryReconciliation.Web/             Blazor app, MudBlazor UI, API endpoints
tests/
  InventoryReconciliation.Tests/           Unit tests for reconciliation behavior
docs/
  Architecture.md
  DatabaseSchema.sql
  AzureDeployment.md
  Security.md
samples/
  ITAssetImportTemplate.xlsx
```

## Run Locally

Install the .NET 9 SDK, then:

```powershell
dotnet restore InventoryReconciliation.sln
dotnet run --project src/InventoryReconciliation.Web/InventoryReconciliation.Web.csproj
```

The development profile uses a local development identity with `SuperAdmin`, `InventoryAdmin`, and `Auditor` roles. Set `Authentication:EnableEntraId` to `true` and configure the `AzureAd` section for real SSO.

## Key URLs

- Web app: `https://localhost:7044`
- Swagger: `https://localhost:7044/swagger`
- Asset API: `/api/assets`
- Import preview API: `/api/imports/preview`
- Reconciliation preview API: `/api/reconcile/preview`

## Azure Readiness

The intended Azure target is:

- Azure App Service for hosting
- Azure SQL for transactional inventory/reconciliation data
- Azure Blob Storage for evidence attachments
- Azure Entra ID for SSO, MFA, groups, and RBAC
- Azure Cache for Redis for dashboard/import cache
- Azure Application Insights and Azure Monitor for telemetry
- Azure Key Vault for connection strings and secrets

See [AzureDeployment.md](docs/AzureDeployment.md) and [Security.md](docs/Security.md).
