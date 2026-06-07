# Run from repo root (DungeonSeeker folder):
#   .\ml-agents\train_ally_position.ps1
#
# Requires Python venv with mlagents package:
#   pip install mlagents

param(
    [string]$RunId = "ally_position_v2_strafe",
    [int]$TimeScale = 20
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ConfigPath = Join-Path $PSScriptRoot "config/ally_position.yaml"

Write-Host "Project root: $ProjectRoot"
Write-Host "Config: $ConfigPath"
Write-Host "Run ID: $RunId"
Write-Host ""
Write-Host "1. Open Unity project"
Write-Host "2. Open scene ML_Training_Arena"
Write-Host "3. Press Play (Behavior Type = Default on ally Behavior Parameters)"
Write-Host "4. Training starts when this script connects"
Write-Host ""

Set-Location $ProjectRoot
mlagents-learn $ConfigPath --run-id=$RunId --time-scale=$TimeScale --force
