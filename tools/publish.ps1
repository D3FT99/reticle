# Builds Reticle into dist/: a self-contained single-file Reticle.exe plus Reticle.Launcher.exe.
# Release builds request uiAccess and only start once signed and installed under Program Files (tools/build-installer.ps1).
# -Version overrides the <Version> in the project files (the release workflow passes the tag's version).
param(
    [string]$Configuration = 'Release',
    [string]$Version
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$out = Join-Path $root 'dist'
$versionArgs = @()
if ($Version) { $versionArgs = @("-p:Version=$Version") }

dotnet publish (Join-Path $root 'src\Reticle\Reticle.csproj') `
    -c $Configuration -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
    @versionArgs -o $out
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)" }

$launcherBuild = Join-Path $root 'src\Reticle.Launcher\bin\publish'
dotnet build (Join-Path $root 'src\Reticle.Launcher\Reticle.Launcher.csproj') -c $Configuration @versionArgs -o $launcherBuild
if ($LASTEXITCODE -ne 0) { throw "launcher build failed ($LASTEXITCODE)" }
Copy-Item (Join-Path $launcherBuild 'Reticle.Launcher.exe') $out -Force

Write-Output "Published to $out"
