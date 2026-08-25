[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$solutionPath = Join-Path (Get-Location) 'ktas-trainer.sln'
if (-not (Test-Path -LiteralPath $solutionPath -PathType Leaf)) {
    $projectPath = (Get-Location).Path
    $versionFile = Join-Path $projectPath 'ProjectSettings/ProjectVersion.txt'
    $versionMatch = Select-String -Path $versionFile -Pattern '^m_EditorVersion: (.+)$'
    if (-not $versionMatch) {
        throw "Unity version could not be determined from $versionFile."
    }

    $unityExecutable = $env:UNITY_EXECUTABLE
    if ([string]::IsNullOrWhiteSpace($unityExecutable)) {
        $version = $versionMatch.Matches[0].Groups[1].Value
        $unityCandidates = @(
            (Join-Path $env:ProgramFiles "Unity/Hub/Editor/$version/Editor/Unity.exe"),
            (Join-Path ${env:ProgramFiles(x86)} "Unity/Hub/Editor/$version/Editor/Unity.exe")
        )
        $unityExecutable = $unityCandidates |
            Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
            Select-Object -First 1
    }

    if ([string]::IsNullOrWhiteSpace($unityExecutable)) {
        throw "Unity was not found. Set UNITY_EXECUTABLE or install the Unity Editor specified in $versionFile."
    }

    & $unityExecutable -batchmode -quit -projectPath $projectPath
    if ($LASTEXITCODE -ne 0) {
        throw "Unity project generation failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-Path -LiteralPath $solutionPath -PathType Leaf)) {
    throw "C# solution was not generated: $solutionPath"
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
