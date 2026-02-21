<#
.SYNOPSIS
    Starts the DICOM ROI Analyzer desktop application.

.DESCRIPTION
    Builds (if needed) and launches the DicomViewer.Desktop WPF application
    using `dotnet run`. By default, opens the SampleFiles\DICOM 20251125 folder.
    Pass -Directory to override, or -NoDirectory to start without loading a folder.

.PARAMETER Directory
    Path to a DICOM folder to load on startup. Defaults to SampleFiles\DICOM 20251125.

.PARAMETER NoDirectory
    Start the application without auto-loading any directory.

.PARAMETER NoBuild
    Skip the build step and run the previously compiled output.

.PARAMETER Configuration
    Build configuration. Defaults to Debug.

.EXAMPLE
    .\Start-Application.ps1

.EXAMPLE
    .\Start-Application.ps1 -Directory "C:\MyDicoms"

.EXAMPLE
    .\Start-Application.ps1 -NoDirectory

.EXAMPLE
    .\Start-Application.ps1 -Configuration Release -NoBuild
#>
[CmdletBinding()]
param(
    [string]$Directory,

    [switch]$NoDirectory,

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

# Resolve the directory to load
if (-not $NoDirectory) {
    if (-not $Directory) {
        $Directory = Join-Path $PSScriptRoot 'SampleFiles' 'DICOM 20251125'
    }
    if (Test-Path $Directory) {
        $arguments += '--'
        $arguments += '--directory'
        $arguments += $Directory
    }
    else {
        Write-Warning "Directory not found: $Directory — starting without auto-load."
    }
}

Write-Host "Starting DicomViewer.Desktop ($Configuration)..." -ForegroundColor Cyan
& dotnet @arguments
