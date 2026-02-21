<#
.SYNOPSIS
    Starts the DICOM ROI Analyzer desktop application.

.DESCRIPTION
    Builds (if needed) and launches the DicomViewer.Desktop WPF application
    using `dotnet run`. Pass -NoBuild to skip the build step when the project
    is already compiled.

.PARAMETER NoBuild
    Skip the build step and run the previously compiled output.

.PARAMETER Configuration
    Build configuration. Defaults to Debug.

.EXAMPLE
    .\Start-DicomRoiAnalyzer.ps1

.EXAMPLE
    .\Start-DicomRoiAnalyzer.ps1 -Configuration Release

.EXAMPLE
    .\Start-DicomRoiAnalyzer.ps1 -NoBuild
#>
[CmdletBinding()]
param(
    [switch]$NoBuild,

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$projectPath = Join-Path $PSScriptRoot 'src' 'DicomViewer.Desktop' 'DicomViewer.Desktop.csproj'

if (-not (Test-Path $projectPath)) {
    Write-Error "Project not found at '$projectPath'. Run this script from the repository root."
    return
}

$arguments = @('run', '--project', $projectPath, '--configuration', $Configuration)

if ($NoBuild) {
    $arguments += '--no-build'
}

Write-Host "Starting DicomViewer.Desktop ($Configuration)..." -ForegroundColor Cyan
& dotnet @arguments
