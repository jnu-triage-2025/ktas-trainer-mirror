[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$solutionPath = Join-Path (Get-Location) 'ktas-trainer.sln'
if (-not (Test-Path -LiteralPath $solutionPath -PathType Leaf)) {
    throw "C# solution was not found: $solutionPath"
}

& dotnet format $solutionPath `
    --include 'Assets/Modules/MultiplayerInfrastructure' `
    --include 'Assets/Modules/TriageTrainer' `
    --exclude 'Assets/Modules/FishNet' `
    --exclude 'Assets/Modules/ParrelSync' `
    --exclude 'Assets/Packages' `
    --severity warn `
    --verify-no-changes

if ($LASTEXITCODE -ne 0) {
    throw "dotnet format failed with exit code $LASTEXITCODE."
}
