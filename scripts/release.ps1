#Requires -Version 5.1
<#
.SYNOPSIS
    Build self-contained single-file llm.exe + zip for Windows x64.

.EXAMPLE
    .\scripts\release.ps1
    .\scripts\release.ps1 -Version 1.0.1
    .\scripts\release.ps1 -SkipHarness
#>
param(
    [string]$Version = "1.0.0",
    [string]$Configuration = "Release",
    [switch]$SkipHarness
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$cliProject = Join-Path $repoRoot "src\LLM.CLI\LLM.CLI.csproj"
$outRoot = Join-Path $repoRoot "artifacts\release"
$publishDir = Join-Path $outRoot "llm-cli-$Version-win-x64"
$zipPath = Join-Path $outRoot "llm-cli-$Version-win-x64.zip"
$shaPath = Join-Path $outRoot "llm-cli-$Version-win-x64.zip.sha256"
$standaloneExe = Join-Path $outRoot "llm-$Version-win-x64.exe"
$standaloneSha = Join-Path $outRoot "llm-$Version-win-x64.exe.sha256"
$wingetInstaller = Join-Path $repoRoot "packaging\winget\manifest\Snigdho48.LLMCLI.installer.yaml"

Write-Host "Release build v$Version (self-contained single-file)" -ForegroundColor Cyan

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

    if (Test-Path $publishDir) {
        Remove-Item $publishDir -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $outRoot | Out-Null

    # Self-contained single-file: users get llm.exe with no separate .NET install.
    dotnet publish $cliProject `
        -c $Configuration `
        -r win-x64 `
        -o $publishDir `
        --self-contained true `
        /p:Version=$Version `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        /p:EnableCompressionInSingleFile=true `
        /p:DebugType=None `
        /p:DebugSymbols=false

    $publishedExe = Join-Path $publishDir "LLM.CLI.exe"
    $exePath = Join-Path $publishDir "llm.exe"
    if (-not (Test-Path $publishedExe)) {
        throw "Publish failed - LLM.CLI.exe missing in $publishDir"
    }

    Move-Item $publishedExe $exePath -Force
    # Drop leftover framework noise if any (single-file should be one main exe + optional pdbs already disabled)
    Get-ChildItem $publishDir -File |
        Where-Object { $_.Name -notin @('llm.exe', 'install.ps1', 'uninstall.ps1', 'README.md', 'LICENSE') -and $_.Extension -in '.dll', '.pdb', '.json' } |
        Remove-Item -Force -ErrorAction SilentlyContinue

    if (-not (Test-Path $exePath)) {
        throw "Publish failed - llm.exe missing in $publishDir"
    }

    # Standalone exe copy for direct download from GitHub Releases.
    Copy-Item $exePath $standaloneExe -Force
    $exeHash = (Get-FileHash -Path $standaloneExe -Algorithm SHA256).Hash.ToUpperInvariant()
    Set-Content -Path $standaloneSha -Value "$exeHash  llm-$Version-win-x64.exe" -Encoding ASCII

    Copy-Item (Join-Path $repoRoot "scripts\install.ps1") $publishDir -Force
    Copy-Item (Join-Path $repoRoot "scripts\uninstall.ps1") $publishDir -Force
    Copy-Item (Join-Path $repoRoot "README.md") $publishDir -Force

    $licensePath = Join-Path $repoRoot "LICENSE"
    if (Test-Path $licensePath) {
        Copy-Item $licensePath $publishDir -Force
    }

    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }

    Compress-Archive -Path "$publishDir\*" -DestinationPath $zipPath -Force

    $hash = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash.ToUpperInvariant()
    Set-Content -Path $shaPath -Value "$hash  llm-cli-$Version-win-x64.zip" -Encoding ASCII

    if (Test-Path $wingetInstaller) {
        $content = Get-Content $wingetInstaller -Raw
        $content = [regex]::Replace(
            $content,
            '(?m)^(\s*InstallerSha256:\s*).+$',
            ('${1}' + $hash))
        $content = [regex]::Replace(
            $content,
            '(?m)^(\s*PackageVersion:\s*).+$',
            ('${1}' + $Version))
        $content = [regex]::Replace(
            $content,
            'llm-cli-[0-9]+\.[0-9]+\.[0-9]+-win-x64\.zip',
            ("llm-cli-$Version-win-x64.zip"))
        $content = [regex]::Replace(
            $content,
            '/download/v[0-9]+\.[0-9]+\.[0-9]+/',
            ("/download/v$Version/"))
        # Prefer llm.exe nested portable entry
        $content = [regex]::Replace(
            $content,
            '(?m)^(\s*RelativeFilePath:\s*).+$',
            '${1}llm.exe')
        Set-Content -Path $wingetInstaller -Value $content.TrimEnd() -Encoding ascii
        Write-Host "[ OK ] Updated winget InstallerSha256" -ForegroundColor Green
    }

    $versionYaml = Join-Path $repoRoot "packaging\winget\manifest\Snigdho48.LLMCLI.yaml"
    if (Test-Path $versionYaml) {
        $v = Get-Content $versionYaml -Raw
        $v = [regex]::Replace($v, '(?m)^(\s*PackageVersion:\s*).+$', ('${1}' + $Version))
        Set-Content -Path $versionYaml -Value $v.TrimEnd() -Encoding ascii
    }

    $localeYaml = Join-Path $repoRoot "packaging\winget\manifest\Snigdho48.LLMCLI.locale.en-US.yaml"
    if (Test-Path $localeYaml) {
        $locale = Get-Content $localeYaml -Raw
        $locale = [regex]::Replace($locale, '(?m)^(\s*PackageVersion:\s*).+$', ('${1}' + $Version))
        $locale = [regex]::Replace(
            $locale,
            'releases/tag/v[0-9]+\.[0-9]+\.[0-9]+',
            ("releases/tag/v$Version"))
        Set-Content -Path $localeYaml -Value $locale.TrimEnd() -Encoding ascii
    }

    Write-Host ""
    Write-Host "[ OK ] Release artifacts:" -ForegroundColor Green
    Write-Host "  $exePath"
    Write-Host "  $standaloneExe"
    Write-Host "  $zipPath"
    Write-Host "  EXE SHA256: $exeHash"
    Write-Host "  ZIP SHA256: $hash"
    Write-Host ""
    Write-Host "Next:"
    Write-Host "  1. Upload llm-$Version-win-x64.exe and the zip to GitHub Release"
    Write-Host "  2. Validate: winget validate --manifest packaging\winget\manifest"
}
finally {
    Pop-Location
}
