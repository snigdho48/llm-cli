#Requires -Version 5.1
<#
.SYNOPSIS
    Installs LLM CLI as a global `llm` command on Windows.

.DESCRIPTION
    Prefer a prebuilt llm.exe next to this script (release zip).
    Otherwise publishes from the repo source into %LOCALAPPDATA%\LLM\cli
    and adds a shim on PATH.

.EXAMPLE
    .\install.ps1
    .\scripts\install.ps1
#>
param(
    [string]$Configuration = "Release",
    [string]$Runtime = ""
)

$ErrorActionPreference = "Stop"

function Get-HostRid {
    try {
        $arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()
    }
    catch {
        $arch = if ([Environment]::Is64BitOperatingSystem) { "X64" } else { "X86" }
    }

    switch ($arch) {
        "X64" { return "win-x64" }
        "X86" { return "win-x86" }
        "Arm64" { return "win-arm64" }
        default { return "win-x64" }
    }
}

$scriptDir = $PSScriptRoot
$repoRoot = Split-Path -Parent $scriptDir
$cliProject = Join-Path $repoRoot "src\LLM.CLI\LLM.CLI.csproj"
$installRoot = Join-Path $env:LOCALAPPDATA "LLM\cli"
$shimDirectory = Join-Path $env:LOCALAPPDATA "LLM\bin"
$rid = if ([string]::IsNullOrWhiteSpace($Runtime)) { Get-HostRid } else { $Runtime }

New-Item -ItemType Directory -Force -Path $installRoot | Out-Null
New-Item -ItemType Directory -Force -Path $shimDirectory | Out-Null

$bundledExe = Join-Path $scriptDir "llm.exe"
$legacyExe = Join-Path $scriptDir "LLM.CLI.exe"

if (Test-Path $bundledExe) {
    Write-Host "Installing bundled llm.exe..." -ForegroundColor Cyan
    Copy-Item $bundledExe (Join-Path $installRoot "llm.exe") -Force
    # Keep compatibility name for older shims.
    Copy-Item $bundledExe (Join-Path $installRoot "LLM.CLI.exe") -Force
}
elseif (Test-Path $legacyExe) {
    Write-Host "Installing bundled LLM.CLI.exe..." -ForegroundColor Cyan
    Copy-Item $legacyExe (Join-Path $installRoot "LLM.CLI.exe") -Force
    Copy-Item $legacyExe (Join-Path $installRoot "llm.exe") -Force
}
elseif (Test-Path $cliProject) {
    Write-Host "Building self-contained llm.exe for $rid ($Configuration)..." -ForegroundColor Cyan
    Push-Location $repoRoot
    try {
        if (Test-Path $installRoot) {
            Get-ChildItem $installRoot -Force | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
        }
        New-Item -ItemType Directory -Force -Path $installRoot | Out-Null

        dotnet publish $cliProject `
            -c $Configuration `
            -r $rid `
            -o $installRoot `
            --self-contained true `
            /p:PublishSingleFile=true `
            /p:IncludeNativeLibrariesForSelfExtract=true `
            /p:EnableCompressionInSingleFile=true `
            /p:DebugType=None `
            /p:DebugSymbols=false
    }
    finally {
        Pop-Location
    }

    $built = Join-Path $installRoot "LLM.CLI.exe"
    if (-not (Test-Path $built)) {
        throw "Publish failed - LLM.CLI.exe not found in $installRoot"
    }

    Copy-Item $built (Join-Path $installRoot "llm.exe") -Force
}
else {
    throw "No llm.exe found next to install.ps1 and no source project to build."
}

$exeName = if (Test-Path (Join-Path $installRoot "llm.exe")) { "llm.exe" } else { "LLM.CLI.exe" }

$shimPath = Join-Path $shimDirectory "llm.cmd"
$shimContent = @"
@echo off
setlocal
set "LLM_CLI_HOME=$installRoot"
"%LLM_CLI_HOME%\$exeName" %*
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
Write-Host "  CLI binary    : $(Join-Path $installRoot $exeName)"
Write-Host "  Global command: llm"
Write-Host ""
Write-Host "Open a new terminal, then run:"
Write-Host "  llm init `"D:\AI`" --auto"
Write-Host "  llm setup --workspace `"D:\AI`" --install-runtime"
Write-Host "  llm serve"
Write-Host "  llm doctor"
