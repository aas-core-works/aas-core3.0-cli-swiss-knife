<#
.SYNOPSIS
This script publishes the binaries and zips them into an archive.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    CreateAndGetArtefactsDir


function Main
{
    Set-Location $PSScriptRoot

    $artefactsDir = CreateAndGetArtefactsDir

    dotnet publish -c Release --output ../out
    $version = ../out/aas-core3.0-sk-verify --version|Out-String
    $version = $version.Trim()

    $releaseDir = Join-Path -Path $artefactsDir -ChildPath "aas-core3.0-sk.${version}"
    if (Test-Path $releaseDir) {
        Remove-Item $releaseDir -Force -Recurse
    }
    New-Item $releaseDir -itemType Directory
    Copy-Item -Path "../out/*.exe" -Destination $releaseDir

    $archivePath = Join-Path -Path $artefactsDir -ChildPath "aas-core3.0-sk.${version}.zip"
    if (Test-Path $archivePath) {
        Remove-Item $archivePath -Force
    }
    Compress-Archive -Path $releaseDir -DestinationPath $archivePath

    Write-Output "Release archived to: ${archivePath}"
}

$previousLocation = Get-Location; try
{
    Main
}
finally
{
    Set-Location $previousLocation
}
