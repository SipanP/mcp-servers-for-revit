<#
.SYNOPSIS
    Assembles the file tree the Inno Setup script installs from.

.DESCRIPTION
    Produces installer/payload/ containing the published server executable once, plus one folder
    per Revit version holding that version's add-in manifest and plugin files.

    The server executable is deliberately excluded from the per-version folders: the installer
    places a single shared copy under the user's Programs directory. Copying an ~80 MB executable
    into all eight would make the installer unusable.
#>
param(
    [Parameter(Mandatory)]
    [string]$ServerPublishDir,

    [string]$PluginBinDir = "plugin/bin",

    [string]$PayloadDir = "installer/payload"
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

$serverExe = Join-Path $ServerPublishDir 'mcp-server-for-revit.exe'
if (-not (Test-Path $serverExe)) {
    throw "Published server not found at $serverExe. Run dotnet publish first."
}

$payload = Join-Path $root $PayloadDir
if (Test-Path $payload) {
    Remove-Item $payload -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $payload | Out-Null

Copy-Item $serverExe -Destination $payload
Write-Host "Payload: mcp-server-for-revit.exe"

$years = 2020..2027
$included = 0

foreach ($year in $years) {
    $configuration = "Release R$(([string]$year).Substring(2))"
    $addinDir = Join-Path $root "$PluginBinDir/AddIn $year $configuration"

    if (-not (Test-Path $addinDir)) {
        Write-Host "Payload: skipping Revit $year (no build output at $addinDir)"
        continue
    }

    $target = Join-Path $payload $year
    New-Item -ItemType Directory -Force -Path $target | Out-Null
    Copy-Item "$addinDir/*" -Destination $target -Recurse -Force

    # Defensive: the release job also bundles the server into the ZIP payloads, and it must not
    # end up duplicated once per Revit version inside the installer.
    Get-ChildItem $target -Recurse -Filter 'mcp-server-for-revit.exe' | Remove-Item -Force

    $included++
    Write-Host "Payload: Revit $year"
}

if ($included -eq 0) {
    throw "No Revit plugin build output was found under $PluginBinDir."
}

Write-Host "Payload assembled at $payload ($included Revit version(s))"
