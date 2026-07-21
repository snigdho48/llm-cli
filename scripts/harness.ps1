#Requires -Version 5.1
param(
    [switch]$Strict,
    [switch]$SkipSmoke
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$results = @()
$failed = 0

function Add-Result {
    param([string]$Id, [bool]$Pass, [string]$Detail)
    $script:results += [PSCustomObject]@{ Id = $Id; Pass = $Pass; Detail = $Detail }
    if (-not $Pass) { $script:failed++ }
    $marker = if ($Pass) { "PASS" } else { "FAIL" }
    Write-Host "[$marker] $Id - $Detail"
}

Write-Host ""
Write-Host "LLM CLI Harness" -ForegroundColor Cyan
Write-Host "==============="
Write-Host ""

Push-Location $repoRoot
try {
    $buildOutput = dotnet build LLM.sln --nologo -v q 2>&1 | Out-String
    $buildOk = $LASTEXITCODE -eq 0
    if ($Strict -and $buildOutput -match "warning") {
        $buildOk = $false
        Add-Result "REG-BUILD" $false "Build has warnings (strict mode)"
    }
    else {
        $detail = if ($buildOk) { "0 errors" } else { "build failed" }
        Add-Result "REG-BUILD" $buildOk $detail
    }

    if (-not $buildOk) {
        Write-Host ""
        Write-Host "HARNESS: FAILED early at build" -ForegroundColor Red
        exit 1
    }

    dotnet test LLM.sln --no-build --nologo -v q 2>&1 | Out-Null
    $testOk = $LASTEXITCODE -eq 0
    $testDetail = if ($testOk) { "all tests pass" } else { "tests failed" }
    Add-Result "REG-TEST" $testOk $testDetail

    if (-not $SkipSmoke) {
        $cliProject = Join-Path $repoRoot "src\LLM.CLI\LLM.CLI.csproj"

        function Invoke-Smoke {
            param([string]$Id, [string[]]$CliArgs, [string]$ExpectPattern)
            $out = dotnet run --project $cliProject --no-build -- @CliArgs 2>&1 | Out-String
            $ok = $LASTEXITCODE -eq 0
            if ($ok -and $ExpectPattern) {
                $ok = $out -match $ExpectPattern
            }
            $detail = if ($ok) { "exit 0" } else { "smoke failed" }
            Add-Result $Id $ok $detail
        }

        Invoke-Smoke "REG-SMOKE-HELP" @("help") "LLM CLI"
        Invoke-Smoke "REG-SMOKE-VERSION" @("version") "Version:"
        Invoke-Smoke "REG-SMOKE-SEARCH" @("model", "search", "coder") "qwen"

        $requiredDocs = @(
            "docs\ARCHITECTURE.md", "docs\SPEC.md", "docs\COMMANDS.md",
            "docs\PRODUCTION_PLAN.md", "docs\ROADMAP.md"
        )
        $docsOk = ($requiredDocs | ForEach-Object { Test-Path (Join-Path $repoRoot $_) }) -notcontains $false
        Add-Result "CAP-DOCS" $docsOk "required docs present"

        $evalsOk = (Test-Path (Join-Path $repoRoot "evals\baseline.json")) -and
                   (Test-Path (Join-Path $repoRoot "scripts\harness.ps1"))
        Add-Result "CAP-HARNESS-SELF" $evalsOk "harness infrastructure"
    }
}
finally {
    Pop-Location
}

Write-Host ""
if ($failed -eq 0) {
    Write-Host "HARNESS: ALL PASS" -ForegroundColor Green
    exit 0
}
else {
    Write-Host "HARNESS: FAILED" -ForegroundColor Red
    exit 1
}
