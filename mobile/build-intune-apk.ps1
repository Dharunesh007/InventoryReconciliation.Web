param(
    [string]$AzureAppUrl = "https://inventoryreconciliation.azurewebsites.net/",
    [string]$Configuration = "Release",
    [string]$ApplicationVersion = "1",
    [string]$ApplicationDisplayVersion = "1.0.0",
    [string]$AndroidSdkDirectory = "$env:LOCALAPPDATA\Android\Sdk"
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $PSScriptRoot "InventoryReconciliation.Mobile\InventoryReconciliation.Mobile.csproj"
$stringsPath = Join-Path $PSScriptRoot "InventoryReconciliation.Mobile\Resources\values\strings.xml"
$outputDir = Join-Path $repoRoot "deployables\intune"
$signingDir = Join-Path $outputDir "signing"
$keystorePath = Join-Path $signingDir "reconiq-intune.keystore"
$credentialPath = Join-Path $signingDir "reconiq-intune-signing.txt"
$alias = "reconiq-intune"
$env:ANDROID_HOME = $AndroidSdkDirectory
$env:ANDROID_SDK_ROOT = $AndroidSdkDirectory

New-Item -ItemType Directory -Force -Path $outputDir, $signingDir | Out-Null

if (-not $AzureAppUrl.StartsWith("https://", [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "AzureAppUrl must be an HTTPS Azure App Service URL, for example https://your-app.azurewebsites.net/"
}

[xml]$strings = Get-Content -LiteralPath $stringsPath
$urlNode = $strings.resources.string | Where-Object { $_.name -eq "azure_app_url" }
if ($null -eq $urlNode) {
    throw "Could not find azure_app_url in $stringsPath"
}

$urlNode.InnerText = $AzureAppUrl.Trim()
$strings.Save($stringsPath)

if (-not (Test-Path -LiteralPath $credentialPath)) {
    $password = -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 28 | ForEach-Object { [char]$_ })
    $credentialText = @"
ReconIQ Android signing credential
Keep this file in a secure password vault. App updates must be signed with the same keystore.

Keystore: $keystorePath
Alias: $alias
Password: $password
"@
    Set-Content -LiteralPath $credentialPath -Value $credentialText -Encoding UTF8
}

$credential = Get-Content -LiteralPath $credentialPath -Raw
$passwordMatch = [regex]::Match($credential, "Password:\s*(?<password>\S+)")
if (-not $passwordMatch.Success) {
    throw "Could not read signing password from $credentialPath"
}

$password = $passwordMatch.Groups["password"].Value

if (-not (Test-Path -LiteralPath $keystorePath)) {
    & keytool -genkeypair `
        -v `
        -keystore $keystorePath `
        -alias $alias `
        -keyalg RSA `
        -keysize 2048 `
        -validity 10000 `
        -storepass $password `
        -keypass $password `
        -dname "CN=ReconIQ Audit Mobile, OU=IT, O=Enterprise, L=Bengaluru, S=Karnataka, C=IN"

    if ($LASTEXITCODE -ne 0) {
        throw "keytool failed to generate the Android signing keystore."
    }
}

& "C:\Program Files\dotnet\dotnet.exe" publish $projectPath `
    -f net9.0-android `
    -c $Configuration `
    -p:AndroidPackageFormats=apk `
    -p:AndroidSdkDirectory=$AndroidSdkDirectory `
    -p:ApplicationVersion=$ApplicationVersion `
    -p:ApplicationDisplayVersion=$ApplicationDisplayVersion `
    -p:AndroidKeyStore=true `
    -p:AndroidSigningKeyStore=$keystorePath `
    -p:AndroidSigningKeyAlias=$alias `
    -p:AndroidSigningStorePass=$password `
    -p:AndroidSigningKeyPass=$password

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed. APK was not created."
}

$apk = Get-ChildItem -Path (Join-Path $PSScriptRoot "InventoryReconciliation.Mobile\bin\$Configuration\net9.0-android\publish") -Filter "*.apk" | Select-Object -First 1
if ($null -eq $apk) {
    throw "Build finished but no APK was found."
}

$finalApk = Join-Path $outputDir "ReconIQ-Audit-Intune-v$ApplicationDisplayVersion.apk"
Copy-Item -LiteralPath $apk.FullName -Destination $finalApk -Force

Write-Host "APK ready: $finalApk"
Write-Host "Signing credential: $credentialPath"
