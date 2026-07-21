#Requires -Version 5.1
<#
.SYNOPSIS
    Removes the global LLM CLI shim from PATH.

.EXAMPLE
    .\scripts\uninstall.ps1
#>
param(
    [switch]$RemoveBinaries
)

$ErrorActionPreference = "Stop"

$installRoot = Join-Path $env:LOCALAPPDATA "LLM\cli"
$shimDirectory = Join-Path $env:LOCALAPPDATA "LLM\bin"
$shimPath = Join-Path $shimDirectory "llm.cmd"

$userPath = [Environment]::GetEnvironmentVariable("Path", "User")
if (-not [string]::IsNullOrWhiteSpace($userPath)) {
    $segments = $userPath -split ";" | Where-Object {
        $_ -and ($_ -ne $shimDirectory)
    }

    $updatedPath = ($segments -join ";").Trim(";")
    [Environment]::SetEnvironmentVariable("Path", $updatedPath, "User")
    Write-Host "Removed from user PATH: $shimDirectory" -ForegroundColor Green
}

if (Test-Path $shimPath) {
    Remove-Item $shimPath -Force
    Write-Host "Removed shim: $shimPath" -ForegroundColor Green
}

if ($RemoveBinaries -and (Test-Path $installRoot)) {
    Remove-Item $installRoot -Recurse -Force
    Write-Host "Removed CLI binaries: $installRoot" -ForegroundColor Green
}

Write-Host ""
Write-Host "Uninstall complete. Open a new terminal for PATH changes to apply." -ForegroundColor Cyan
