#Requires -Version 5.1
<#
.SYNOPSIS
    Build self-contained single-file llm.exe + zip for Windows (x64, x86, arm64).

.EXAMPLE
    .\scripts\release.ps1
    .\scripts\release.ps1 -Version 1.0.2
    .\scripts\release.ps1 -SkipHarness -Runtimes win-x64,win-arm64
#>
param(
    [string]$Version = "1.0.2",
    [string]$Configuration = "Release",
    [string[]]$Runtimes = @("win-x64", "win-x86", "win-arm64"),
    [switch]$SkipHarness
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$cliProject = Join-Path $repoRoot "src\LLM.CLI\LLM.CLI.csproj"
$outRoot = Join-Path $repoRoot "artifacts\release"
$wingetInstaller = Join-Path $repoRoot "packaging\winget\manifest\Snigdho48.LLMCLI.installer.yaml"
$wingetVersion = Join-Path $repoRoot "packaging\winget\manifest\Snigdho48.LLMCLI.yaml"
$wingetLocale = Join-Path $repoRoot "packaging\winget\manifest\Snigdho48.LLMCLI.locale.en-US.yaml"

$ridToWingetArch = @{
    "win-x64"   = "x64"
    "win-x86"   = "x86"
    "win-arm64" = "arm64"
}

function Get-KeepNames {
    return @("llm.exe", "install.ps1", "uninstall.ps1", "README.md", "LICENSE")
}

function Publish-OneRuntime {
    param(
        [string]$Rid,
        [string]$Version,
        [string]$Configuration
    )

    $label = $Rid
    $publishDir = Join-Path $outRoot "llm-cli-$Version-$Rid"
    $zipPath = Join-Path $outRoot "llm-cli-$Version-$Rid.zip"
    $shaPath = Join-Path $outRoot "llm-cli-$Version-$Rid.zip.sha256"
    $standaloneExe = Join-Path $outRoot "llm-$Version-$Rid.exe"
    $standaloneSha = Join-Path $outRoot "llm-$Version-$Rid.exe.sha256"

    Write-Host ""
    Write-Host "=== Publishing $label ===" -ForegroundColor Cyan

    if (Test-Path $publishDir) {
        Remove-Item $publishDir -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $publishDir | Out-Null

    dotnet publish $cliProject `
        -c $Configuration `
        -r $Rid `
        -o $publishDir `
        --self-contained true `
        /p:Version=$Version `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        /p:EnableCompressionInSingleFile=true `
        /p:DebugType=None `
        /p:DebugSymbols=false

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for $Rid"
    }

    $publishedExe = Join-Path $publishDir "LLM.CLI.exe"
    $exePath = Join-Path $publishDir "llm.exe"
    if (-not (Test-Path $publishedExe)) {
        throw "Publish failed - LLM.CLI.exe missing for $Rid"
    }

    Move-Item $publishedExe $exePath -Force

    $keep = Get-KeepNames
    Get-ChildItem $publishDir -File |
        Where-Object {
            $_.Name -notin $keep -and
            $_.Extension -in ".dll", ".pdb", ".json", ".xml"
        } |
        Remove-Item -Force -ErrorAction SilentlyContinue

    Copy-Item (Join-Path $repoRoot "scripts\install.ps1") $publishDir -Force
    Copy-Item (Join-Path $repoRoot "scripts\uninstall.ps1") $publishDir -Force
    Copy-Item (Join-Path $repoRoot "README.md") $publishDir -Force
    $licensePath = Join-Path $repoRoot "LICENSE"
    if (Test-Path $licensePath) {
        Copy-Item $licensePath $publishDir -Force
    }

    Copy-Item $exePath $standaloneExe -Force
    $exeHash = (Get-FileHash -Path $standaloneExe -Algorithm SHA256).Hash.ToUpperInvariant()
    Set-Content -Path $standaloneSha -Value "$exeHash  llm-$Version-$Rid.exe" -Encoding ASCII

    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }

    Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath -Force
    $zipHash = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash.ToUpperInvariant()
    Set-Content -Path $shaPath -Value "$zipHash  llm-cli-$Version-$Rid.zip" -Encoding ASCII

    Write-Host "[ OK ] $Rid exe=$exeHash zip=$zipHash" -ForegroundColor Green

    return [PSCustomObject]@{
        Rid      = $Rid
        Arch     = $ridToWingetArch[$Rid]
        ZipName  = "llm-cli-$Version-$Rid.zip"
        ExeName  = "llm-$Version-$Rid.exe"
        ZipHash  = $zipHash
        ExeHash  = $exeHash
        ZipPath  = $zipPath
        ExePath  = $standaloneExe
    }
}

Write-Host "Release build v$Version (self-contained: $($Runtimes -join ', '))" -ForegroundColor Cyan

Push-Location $repoRoot
try {
    if (-not $SkipHarness) {
        & (Join-Path $repoRoot "scripts\harness.ps1") -SkipSmoke
        if ($LASTEXITCODE -ne 0) {
            throw "Harness failed - fix before release."
        }
    }
    else {
        Write-Host "[WARN] Skipping harness (-SkipHarness)." -ForegroundColor Yellow
    }

    New-Item -ItemType Directory -Force -Path $outRoot | Out-Null

    $results = New-Object System.Collections.Generic.List[object]
    foreach ($rid in $Runtimes) {
        if (-not $ridToWingetArch.ContainsKey($rid)) {
            throw "Unsupported runtime '$rid'. Use: win-x64, win-x86, win-arm64"
        }

        $null = $results.Add((Publish-OneRuntime -Rid $rid -Version $Version -Configuration $Configuration))
    }

    # Refresh winget multi-arch installer manifest
    if (Test-Path $wingetInstaller) {
        $installerBlocks = foreach ($item in $results) {
            "  - Architecture: $($item.Arch)`n    InstallerUrl: https://github.com/snigdho48/llm-cli/releases/download/v$Version/$($item.ZipName)`n    InstallerSha256: $($item.ZipHash)"
        }

        $wingetBody = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.installer.1.9.0.schema.json
# InstallerSha256 values filled by scripts/release.ps1
# Self-contained single-file llm.exe - no .NET Runtime dependency.
PackageIdentifier: Snigdho48.LLMCLI
PackageVersion: $Version
Platform:
  - Windows.Desktop
MinimumOSVersion: 10.0.17763.0
InstallerType: zip
NestedInstallerType: portable
NestedInstallerFiles:
  - RelativeFilePath: llm.exe
    PortableCommandAlias: llm
Commands:
  - llm
UpgradeBehavior: install
ReleaseDate: $((Get-Date).ToString('yyyy-MM-dd'))
Installers:
$($installerBlocks -join "`r`n")
ManifestType: installer
ManifestVersion: 1.9.0
"@
        Set-Content -Path $wingetInstaller -Value $wingetBody.TrimEnd() -Encoding ascii
        Write-Host "[ OK ] Updated winget multi-arch manifest ($($results.Count) installers)" -ForegroundColor Green
    }

    if (Test-Path $wingetVersion) {
        $v = Get-Content $wingetVersion -Raw
        $v = [regex]::Replace($v, '(?m)^(\s*PackageVersion:\s*).+$', ('${1}' + $Version))
        Set-Content -Path $wingetVersion -Value $v.TrimEnd() -Encoding ascii
    }

    if (Test-Path $wingetLocale) {
        $locale = Get-Content $wingetLocale -Raw
        $locale = [regex]::Replace($locale, '(?m)^(\s*PackageVersion:\s*).+$', ('${1}' + $Version))
        $locale = [regex]::Replace(
            $locale,
            'releases/tag/v[0-9]+\.[0-9]+\.[0-9]+',
            ("releases/tag/v$Version"))
        Set-Content -Path $wingetLocale -Value $locale.TrimEnd() -Encoding ascii
    }

    Write-Host ""
    Write-Host "[ OK ] Release artifacts ($($results.Count) architectures):" -ForegroundColor Green
    foreach ($item in $results) {
        Write-Host "  $($item.ExePath)"
        Write-Host "  $($item.ZipPath)"
    }

    Write-Host ""
    Write-Host "Next:"
    Write-Host "  1. Upload all llm-*-win-*.exe and llm-cli-*-win-*.zip to GitHub Release"
    Write-Host "  2. Validate: winget validate --manifest packaging\winget\manifest"
}
finally {
    Pop-Location
}
