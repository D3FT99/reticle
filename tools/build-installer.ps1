<#
.SYNOPSIS
    Builds the signed Reticle installer: dist\Reticle-<version>-setup.exe.

.DESCRIPTION
    1. Uses (or creates on first run) a self-signed code-signing certificate "CN=Reticle" in Cert:\CurrentUser\My.
       No administrator rights needed; the private key is non-exportable and never written to disk.
    2. Publishes dist\Reticle.exe and dist\Reticle.Launcher.exe (tools\publish.ps1).
    3. Signs both exes (SHA-256, RFC 3161 timestamp) and exports the public certificate for the installer.
    4. Compiles installer\Reticle.iss with Inno Setup 6 and signs the resulting setup exe.

    The installer trusts the certificate on the machine it runs on (required for uiAccess), so only install it on
    machines you control.

    Requires Inno Setup 6: winget install JRSoftware.InnoSetup

.PARAMETER Version
    Overrides the <Version> in src\Reticle\Reticle.csproj. The release workflow passes the tag's version.

.PARAMETER NoCreateCert
    Fail instead of creating a certificate when none is found (used in CI, where the certificate comes from a secret).
#>
param(
    [string]$Subject = 'CN=Reticle',
    [string]$TimestampServer = 'http://timestamp.digicert.com',
    [string]$Version,
    [switch]$SkipPublish,
    [switch]$NoCreateCert
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$dist = Join-Path $root 'dist'

function Sign([string]$File) {
    $result = Set-AuthenticodeSignature $File -Certificate $cert -HashAlgorithm SHA256 -TimestampServer $TimestampServer
    if (-not $result.SignerCertificate) { throw "Signing $File failed: $($result.StatusMessage)" }
    Write-Output "Signed $(Split-Path $File -Leaf)"
}

$iscc = @(
    (Get-Command ISCC.exe -ErrorAction SilentlyContinue).Source
    Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'
    Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'
    Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe'
) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $iscc) { throw 'Inno Setup 6 not found. Install it with: winget install JRSoftware.InnoSetup' }

# 1. Certificate
$cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert |
    Where-Object { $_.Subject -eq $Subject -and $_.NotAfter -gt (Get-Date) } |
    Sort-Object NotAfter -Descending | Select-Object -First 1
if (-not $cert) {
    if ($NoCreateCert) { throw "No valid code-signing certificate $Subject in Cert:\CurrentUser\My" }
    Write-Output "Creating code-signing certificate $Subject"
    $cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject $Subject `
        -CertStoreLocation Cert:\CurrentUser\My -KeyExportPolicy NonExportable -NotAfter (Get-Date).AddYears(5)
}
Write-Output "Certificate $($cert.Subject) ($($cert.Thumbprint)), valid until $($cert.NotAfter.ToShortDateString())"

# 2. Publish
if (-not $SkipPublish) {
    & (Join-Path $PSScriptRoot 'publish.ps1') -Configuration Release -Version $Version
}

# 3. Sign the exes and export the public certificate
Sign (Join-Path $dist 'Reticle.exe')
Sign (Join-Path $dist 'Reticle.Launcher.exe')
$cer = Join-Path $dist 'Reticle-signing.cer'
Export-Certificate -Cert $cert -FilePath $cer | Out-Null

# 4. Compile and sign the installer
$version = $Version
if (-not $version) {
    [xml]$project = Get-Content (Join-Path $root 'src\Reticle\Reticle.csproj')
    $version = ($project.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version
}
& $iscc /Q "/DAppVersion=$version" "/DCertFile=$cer" "/DCertThumbprint=$($cert.Thumbprint)" (Join-Path $root 'installer\Reticle.iss')
if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed ($LASTEXITCODE)" }
$setup = Join-Path $dist "Reticle-$version-setup.exe"
Sign $setup
Write-Output "Installer: $setup"
