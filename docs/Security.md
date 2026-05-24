# Security Design

## Identity

- Azure Entra ID SSO using OpenID Connect.
- MFA enforced through Conditional Access.
- Entra app roles mapped to platform RBAC.
- Development mode uses an explicit fallback auth handler only when `Authentication:EnableEntraId=false`.

## RBAC Matrix

| Permission | Super Admin | Inventory Admin | Auditor | Regional Manager | IT Support | Executive | Compliance |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Import inventory | Yes | Yes | No | No | No | No | No |
| Verify assets | Yes | Yes | Yes | No | Yes | No | No |
| Approve reconciliation | Yes | Yes | No | Yes | No | No | Yes |
| View executive reports | Yes | Yes | No | Yes | No | Yes | Yes |
| Manage roles/settings | Yes | No | No | No | No | No | No |
| Export compliance evidence | Yes | Yes | No | Yes | No | Yes | Yes |

## Data Protection

- SQL injection protection through EF Core parameterized queries.
- File uploads restricted to `.xlsx` for imports and content-type validated attachments.
- Evidence files stored in Azure Blob Storage; SQL stores metadata only.
- TLS-only transport.
- Key Vault for secrets and connection strings.
- App Insights avoids logging raw uploaded workbook values or PII-heavy payloads.

## Audit Controls

- All asset changes append `AssetAuditLog`.
- Previous and new values are retained.
- Verification records store device fingerprint and optional geo-location.
- Approval status and decision metadata are separate from source asset data.
- Imports create versioned `InventorySnapshot` records.

## OWASP Controls

- Authentication and authorization applied to API group.
- Anti-forgery enabled for Blazor app.
- Upload size limit enforced for import preview endpoint.
- Secure headers should be added at App Service or middleware layer.
- Centralized exception handling in production.
- API throttling should be added with `AspNetCoreRateLimit` or Azure Front Door/WAF for internet-exposed deployments.

## Row-Level Security

`docs/DatabaseSchema.sql` includes an optional SQL Server row-level security pattern using `SESSION_CONTEXT('Region')`. In production, middleware should set session context after authentication for regional managers/auditors.

## Recommended Hardening Before Go-Live

- Replace storage connection string with managed identity and `DefaultAzureCredential`.
- Add malware scanning for uploaded evidence and workbooks.
- Add content security policy headers.
- Add rate limiting middleware.
- Encrypt offline PWA cache for mobile verification.
- Add data retention policies for attachments and audit exports.
- Add automated access review for privileged roles.
