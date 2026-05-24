# Azure Deployment Guide

## Azure Resources

Create these resources per environment:

- Resource group: `rg-inventory-reconciliation-prod`
- App Service Plan: Linux or Windows, Premium v3 recommended
- App Service: `app-inventory-reconciliation-prod`
- Azure SQL Server and database
- Azure Storage account + private container `asset-evidence`
- Azure Cache for Redis
- Application Insights workspace-based resource
- Key Vault
- Azure Entra ID app registration

## App Registration

1. Create an Entra ID app registration.
2. Add redirect URI: `https://<app-name>.azurewebsites.net/signin-oidc`.
3. Enable ID tokens.
4. Create app roles matching:
   - `SuperAdmin`
   - `InventoryAdmin`
   - `Auditor`
   - `RegionalManager`
   - `ItSupport`
   - `ReadOnlyExecutive`
   - `ComplianceTeam`
5. Assign users or groups to app roles.

## Configuration

Use App Service configuration or Key Vault references:

```text
Authentication__EnableEntraId=true
AzureAd__TenantId=<tenant-id>
AzureAd__ClientId=<client-id>
AzureAd__Domain=<tenant-domain>
ConnectionStrings__InventoryDb=<azure-sql-connection-string>
ConnectionStrings__Redis=<redis-connection-string>
ConnectionStrings__BlobStorage=<storage-connection-string>
ApplicationInsights__ConnectionString=<app-insights-connection-string>
Storage__AttachmentContainer=asset-evidence
```

## Database

Preferred production path:

```powershell
dotnet ef migrations add InitialCreate --project src/InventoryReconciliation.Infrastructure --startup-project src/InventoryReconciliation.Web
dotnet ef database update --project src/InventoryReconciliation.Infrastructure --startup-project src/InventoryReconciliation.Web
```

The schema reference is available at `docs/DatabaseSchema.sql`.

## Deployment Commands

```powershell
dotnet publish src/InventoryReconciliation.Web/InventoryReconciliation.Web.csproj -c Release -o publish
az webapp deploy --resource-group rg-inventory-reconciliation-prod --name app-inventory-reconciliation-prod --src-path publish.zip --type zip
```

## Operational Settings

- Enable Always On.
- Configure health check endpoint when a health probe is added.
- Enforce HTTPS only.
- Use managed identity for SQL, Storage, and Key Vault where possible.
- Configure private endpoints for SQL and Storage in regulated environments.
- Set App Service authentication to allow app-managed OpenID Connect flow.

## Monitoring

Track:

- Import duration and rejected row count.
- Verification throughput by auditor/site.
- Reconciliation outcome distribution.
- Critical mismatch SLA aging.
- API latency and SQL dependency duration.
- Blob upload failures.
- Authentication failures and forbidden access attempts.
