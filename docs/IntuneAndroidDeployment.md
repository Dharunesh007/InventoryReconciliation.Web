# ReconIQ Audit Android Intune Package

This mobile package is a separate Android APK wrapper for the Azure-hosted Inventory Physical Verification & Reconciliation web platform.

## Build

Run from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\mobile\build-intune-apk.ps1 `
  -AzureAppUrl "https://<your-app-service-name>.azurewebsites.net/" `
  -ApplicationVersion "1" `
  -ApplicationDisplayVersion "1.0.0"
```

The APK is written to:

```text
deployables\intune\ReconIQ-Audit-Intune-v1.0.0.apk
```

## Azure Requirements

- Host the Blazor app on Azure App Service using HTTPS.
- Configure Entra ID redirect URI for the web app: `https://<your-app-service-name>.azurewebsites.net/signin-oidc`.
- Keep `Authentication__EnableEntraId=true` in Azure App Service configuration when moving beyond local development.
- If the app is restricted to private networks, require device VPN or Microsoft Tunnel before launching the mobile app.

## Intune Upload Options

Preferred for Android Enterprise:

1. Intune admin center > Apps > All apps > Create.
2. Select `Managed Google Play app`.
3. Open `Private apps`.
4. Upload `ReconIQ-Audit-Intune-v1.0.0.apk`.
5. Sync the app into Intune and assign it to the target Azure Entra device/user groups.
6. Add an Android Enterprise app configuration policy and set `azure_app_url` to your Azure App Service URL.

Direct APK deployment:

1. Intune admin center > Apps > All apps > Create.
2. Platform: `Android`.
3. App type: `Line-of-business app`.
4. Upload `ReconIQ-Audit-Intune-v1.0.0.apk`.
5. Assign as required or available for the intended device group.

## Managed App Configuration

The APK exposes this managed configuration key:

| Key | Type | Example |
| --- | --- | --- |
| `azure_app_url` | String | `https://<your-app-service-name>.azurewebsites.net/` |

If Intune supplies this value, the mobile app uses it. If no policy is present, it falls back to the URL compiled into `Resources\values\strings.xml`.

## Versioning

Increase `ApplicationVersion` for every Intune update. Android/Intune treat this as the version code, and upgrades require it to be higher than the installed package.

## Signing

The build script creates a local keystore under:

```text
deployables\intune\signing
```

Keep the keystore and password in a secure vault. Future app updates must use the same signing key.

## Package Identity

```text
Package ID: com.reconiq.inventoryreconciliation
Display name: ReconIQ Audit
Minimum Android: 8.0 / API 26
```
