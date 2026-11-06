#Requires -Version 7.0
<#
.SYNOPSIS
    Downloads the third-party binaries that are bundled into the installer.

.DESCRIPTION
    The binaries are intentionally not stored in git. Every download is pinned to an exact
    version and verified against a SHA-256 checksum, so a compromised or replaced release
    asset fails the build instead of shipping.

    VIIPER      GPL-3.0     https://github.com/Alia5/VIIPER
    usbip-win2  BSD-2-Clause https://github.com/vadimgrn/usbip-win2
#>
param(
    [string]$Destination = (Join-Path $PSScriptRoot "..\thirdparty")
)

$ErrorActionPreference = "Stop"

$items = @(
    @{
        Name     = "viiper.exe"
        Url      = "https://github.com/Alia5/VIIPER/releases/download/v0.9.0/viiper-windows-amd64.exe"
        Sha256   = "142c144f6abee8b8a5f6a1abbc7018683444de71d2e45102cde6aaab9b2223e6"
        Directory = "viiper"
        Version  = "v0.9.0"
    },
    @{
        Name     = "USBip-0.9.8.1-x64.exe"
        Url      = "https://github.com/vadimgrn/usbip-win2/releases/download/v.0.9.8.1/USBip-0.9.8.1-x64.exe"
        Sha256   = "38cad6d4432b52d5bb9409d9ad03b72fdffc4ada4cd3a48fbeca1a2752a8518a"
        Directory = "usbip-win2"
        Version  = "v0.9.8.1"
    }
)

foreach ($item in $items) {
    $targetDirectory = Join-Path $Destination $item.Directory
    New-Item -ItemType Directory -Force -Path $targetDirectory | Out-Null
    $target = Join-Path $targetDirectory $item.Name

    if (Test-Path $target) {
        $existing = (Get-FileHash -Path $target -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($existing -eq $item.Sha256) {
            Write-Host "third-party: $($item.Name) already present and verified."
            continue
        }
        Remove-Item -Path $target -Force
    }

    Write-Host "third-party: downloading $($item.Name) ($($item.Version))..."
    Invoke-WebRequest -Uri $item.Url -OutFile $target -UseBasicParsing

    $actual = (Get-FileHash -Path $target -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -ne $item.Sha256) {
        Remove-Item -Path $target -Force
        throw "Checksum mismatch for $($item.Name). Expected $($item.Sha256), got $actual."
    }

    Write-Host "third-party: $($item.Name) verified."
}
