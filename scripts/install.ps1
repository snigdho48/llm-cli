#Requires -Version 5.1
<#
.SYNOPSIS
    Builds and installs the LLM CLI as a global `llm` command on Windows.

.DESCRIPTION
    Publishes LLM.CLI to %LOCALAPPDATA%\LLM\cli and adds a shim directory
    to the current user's PATH. Run from the repository root.

.EXAMPLE
    .\scripts\install.ps1
#>
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$cliProject = Join-Path $repoRoot "src\LLM.CLI\LLM.CLI.csproj"
$installRoot = Join-Path $env:LOCALAPPDATA "LLM\cli"
$shimDirectory = Join-Path $env:LOCALAPPDATA "LLM\bin"

Write-Host "Building LLM CLI ($Configuration)..." -ForegroundColor Cyan
Push-Location $repoRoot
try {
    dotnet publish $cliProject `
        -c $Configuration `
        -o $installRoot `
        --self-contained false `
        /p:PublishSingleFile=false
}
finally {
    Pop-Location
}

New-Item -ItemType Directory -Force -Path $shimDirectory | Out-Null

$shimPath = Join-Path $shimDirectory "llm.cmd"
$shimContent = @"
@echo off
setlocal
set "LLM_CLI_HOME=$installRoot"
"%LLM_CLI_HOME%\LLM.CLI.exe" %*
"@

Set-Content -Path $shimPath -Value $shimContent -Encoding ASCII

$userPath = [Environment]::GetEnvironmentVariable("Path", "User")
if ($userPath -notlike "*$shimDirectory*") {
    $updatedPath = if ([string]::IsNullOrWhiteSpace($userPath)) {
        $shimDirectory
    }
    else {
        "$userPath;$shimDirectory"
    }

    [Environment]::SetEnvironmentVariable("Path", $updatedPath, "User")
    $env:Path = "$env:Path;$shimDirectory"
    Write-Host "Added to user PATH: $shimDirectory" -ForegroundColor Green
}
else {
    Write-Host "Shim directory already on PATH: $shimDirectory" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Installed successfully." -ForegroundColor Green
Write-Host "  CLI binaries : $installRoot"
Write-Host "  Global command: llm"
Write-Host ""
Write-Host "Open a new terminal, then run:"
Write-Host "  llm init `"D:\AI`" --auto"
Write-Host "  llm setup --workspace `"D:\AI`" --install-runtime --model `"D:\MODEL\your-model.gguf`" --no-start"
Write-Host "  # or: llm runtime install"
Write-Host "  llm serve"
Write-Host "  llm doctor"
