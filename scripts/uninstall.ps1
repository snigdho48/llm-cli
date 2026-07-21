#Requires -Version 5.1
<#
.SYNOPSIS
    Removes the global LLM CLI from PATH and optional binaries.

.EXAMPLE
    .\scripts\uninstall.ps1
    .\scripts\uninstall.ps1 -RemoveBinaries
#>
param(
    [switch]$RemoveBinaries
)

$ErrorActionPreference = "Stop"

$installRoot = Join-Path $env:LOCALAPPDATA "LLM\cli"
$binDirectory = Join-Path $env:LOCALAPPDATA "LLM\bin"
$shimPath = Join-Path $binDirectory "llm.cmd"
$binExe = Join-Path $binDirectory "llm.exe"

$userPath = [Environment]::GetEnvironmentVariable("Path", "User")
if (-not [string]::IsNullOrWhiteSpace($userPath)) {
    $segments = $userPath -split ";" | Where-Object {
        $_ -and ($_ -ne $binDirectory)
    }

    $updatedPath = ($segments -join ";").Trim(";")
    [Environment]::SetEnvironmentVariable("Path", $updatedPath, "User")
    Write-Host "Removed from user PATH: $binDirectory" -ForegroundColor Green
}

foreach ($path in @($shimPath, $binExe)) {
    if (Test-Path $path) {
        Remove-Item $path -Force
        Write-Host "Removed: $path" -ForegroundColor Green
    }
}

if ($RemoveBinaries -and (Test-Path $installRoot)) {
    Remove-Item $installRoot -Recurse -Force
    Write-Host "Removed CLI binaries: $installRoot" -ForegroundColor Green
}

Write-Host ""
Write-Host "Uninstall complete. Open a new terminal for PATH changes to apply." -ForegroundColor Cyan
