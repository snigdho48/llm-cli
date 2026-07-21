#Requires -Version 5.1
<#
.SYNOPSIS
    Installs LLM CLI as a global `llm` command on Windows.

.DESCRIPTION
    Copies llm.exe into %LOCALAPPDATA%\LLM\bin (on PATH) and %LOCALAPPDATA%\LLM\cli.
    Accepts a bundled llm.exe, a versioned release exe (llm-*-win-*.exe), or builds from source.

.EXAMPLE
    .\install.ps1
    .\scripts\install.ps1
    .\install.ps1 -ExePath .\llm-1.0.4-win-x64.exe
#>
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "",
    [string]$ExePath = ""
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

function Find-ReleaseExe {
    param([string]$Directory, [string]$Rid)

    $exact = Join-Path $Directory "llm.exe"
    if (Test-Path $exact) { return $exact }

    $legacy = Join-Path $Directory "LLM.CLI.exe"
    if (Test-Path $legacy) { return $legacy }

    $ridMatch = Get-ChildItem -Path $Directory -Filter "llm-*-$Rid.exe" -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if ($ridMatch) { return $ridMatch.FullName }

    $any = Get-ChildItem -Path $Directory -Filter "llm-*-win-*.exe" -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if ($any) { return $any.FullName }

    return $null
}

$scriptDir = $PSScriptRoot
$repoRoot = Split-Path -Parent $scriptDir
$cliProject = Join-Path $repoRoot "src\LLM.CLI\LLM.CLI.csproj"
$installRoot = Join-Path $env:LOCALAPPDATA "LLM\cli"
$binDirectory = Join-Path $env:LOCALAPPDATA "LLM\bin"
$rid = if ([string]::IsNullOrWhiteSpace($Runtime)) { Get-HostRid } else { $Runtime }

New-Item -ItemType Directory -Force -Path $installRoot | Out-Null
New-Item -ItemType Directory -Force -Path $binDirectory | Out-Null

$sourceExe = $null
if (-not [string]::IsNullOrWhiteSpace($ExePath)) {
    if (-not (Test-Path $ExePath)) {
        throw "ExePath not found: $ExePath"
    }
    $sourceExe = (Resolve-Path $ExePath).Path
}
else {
    $sourceExe = Find-ReleaseExe -Directory $scriptDir -Rid $rid
    if (-not $sourceExe) {
        $sourceExe = Find-ReleaseExe -Directory (Get-Location).Path -Rid $rid
    }
}

if ($sourceExe) {
    Write-Host "Installing $($sourceExe)..." -ForegroundColor Cyan
    Copy-Item $sourceExe (Join-Path $installRoot "llm.exe") -Force
    Copy-Item $sourceExe (Join-Path $installRoot "LLM.CLI.exe") -Force
    Copy-Item $sourceExe (Join-Path $binDirectory "llm.exe") -Force
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
    Copy-Item $built (Join-Path $binDirectory "llm.exe") -Force
}
else {
    throw "No llm.exe / llm-*-win-*.exe found next to install.ps1 and no source project to build."
}

$shimPath = Join-Path $binDirectory "llm.cmd"
$shimContent = @"
@echo off
setlocal
"%~dp0llm.exe" %*
"@
Set-Content -Path $shimPath -Value $shimContent -Encoding ASCII

$userPath = [Environment]::GetEnvironmentVariable("Path", "User")
if ($userPath -notlike "*$binDirectory*") {
    $updatedPath = if ([string]::IsNullOrWhiteSpace($userPath)) {
        $binDirectory
    }
    else {
        "$userPath;$binDirectory"
    }

    [Environment]::SetEnvironmentVariable("Path", $updatedPath, "User")
    $env:Path = "$env:Path;$binDirectory"
    Write-Host "Added to user PATH: $binDirectory" -ForegroundColor Green
}
else {
    Write-Host "Install directory already on PATH: $binDirectory" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Installed successfully." -ForegroundColor Green
Write-Host "  CLI binary    : $(Join-Path $binDirectory 'llm.exe')"
Write-Host "  Global command: llm"
Write-Host ""
Write-Host "Open a new terminal, then run:"
Write-Host "  llm version"
Write-Host "  llm init `"D:\AI`" --auto"
Write-Host "  llm serve"
