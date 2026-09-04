[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$projectRoot = (Get-Location).Path
$solutionPath = Join-Path $projectRoot 'ktas-trainer.sln'
$artifactsPath = Join-Path $projectRoot 'artifacts'
New-Item -ItemType Directory -Path $artifactsPath -Force | Out-Null
$formatTarget = $solutionPath
if (-not (Test-Path -LiteralPath $formatTarget -PathType Leaf)) {
    $projectPath = $projectRoot
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

    $unityRuntimePath = Join-Path $projectPath 'Temp/azure-unity-runtime'
    $unityLocalAppData = Join-Path $unityRuntimePath 'LocalAppData'
    $unityTempPath = Join-Path $unityRuntimePath 'Temp'
    $unityLogPath = Join-Path $artifactsPath 'unity-project-generation.log'
    New-Item -ItemType Directory -Path $unityLocalAppData, $unityTempPath -Force | Out-Null
    $env:LOCALAPPDATA = $unityLocalAppData
    $env:TEMP = $unityTempPath
    $env:TMP = $unityTempPath

    $unityArguments = @(
        '-batchmode',
        '-quit',
        '-projectPath',
        $projectPath,
        '-logFile',
        $unityLogPath
    )
    $unityProcess = Start-Process -FilePath $unityExecutable `
        -ArgumentList $unityArguments `
        -WorkingDirectory $projectPath `
        -Wait `
        -PassThru
    if ($unityProcess.ExitCode -ne 0) {
        if (Test-Path -LiteralPath $unityLogPath -PathType Leaf) {
            Get-Content -LiteralPath $unityLogPath -Tail 200
        }
        throw "Unity project generation failed with exit code $($unityProcess.ExitCode). See $unityLogPath."
    }
}

if (-not (Test-Path -LiteralPath $formatTarget -PathType Leaf)) {
    $formatTarget = @(
        (Join-Path $projectRoot 'Assembly-CSharp.csproj'),
        (Join-Path $projectRoot 'Assembly-CSharp-Editor.csproj')
    ) | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($formatTarget)) {
    throw "No generated C# solution or project was found under $projectRoot."
}

& dotnet format $formatTarget `
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
