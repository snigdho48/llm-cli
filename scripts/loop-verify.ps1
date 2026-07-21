#Requires -Version 5.1
<#
.SYNOPSIS
    Continuous verification loop — re-runs harness on interval (loop engineering).

.EXAMPLE
    .\scripts\loop-verify.ps1 -IntervalMinutes 5
    .\scripts\loop-verify.ps1 -Once
#>
param(
    [int]$IntervalMinutes = 5,
    [switch]$Once
)

$ErrorActionPreference = "Stop"
$harness = Join-Path (Split-Path -Parent $PSScriptRoot) "scripts\harness.ps1"
$run = 0
$consecutivePass = 0
$targetPass = 3

Write-Host "Loop verify: harness every $IntervalMinutes minute(s). Target pass^$targetPass for release." -ForegroundColor Cyan
Write-Host "Press Ctrl+C to stop."
Write-Host ""

do {
    $run++
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Write-Host "--- Loop tick #$run @ $timestamp ---" -ForegroundColor Yellow

    & $harness
    if ($LASTEXITCODE -eq 0) {
        $consecutivePass++
        Write-Host "Consecutive passes: $consecutivePass / $targetPass" -ForegroundColor Green
        if ($consecutivePass -ge $targetPass) {
            Write-Host ""
            Write-Host "pass^$targetPass achieved — release gate satisfied." -ForegroundColor Green
        }
    }
    else {
        $consecutivePass = 0
        Write-Host "Consecutive passes reset to 0." -ForegroundColor Red
    }

    Write-Host ""
    if ($Once) { break }

    Start-Sleep -Seconds ($IntervalMinutes * 60)
} while ($true)
