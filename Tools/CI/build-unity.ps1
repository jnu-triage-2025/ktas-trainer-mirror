[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($env:BUILD_TARGET)) {
    throw 'BUILD_TARGET must be set to a Unity BuildTarget.'
}

if ([string]::IsNullOrWhiteSpace($env:BUILD_NAME)) {
    $env:BUILD_NAME = 'ktas-trainer'
}

if ([string]::IsNullOrWhiteSpace($env:BUILD_PATH)) {
    $env:BUILD_PATH = 'build'
}

$projectPath = if ($env:CI_PROJECT_DIR) { $env:CI_PROJECT_DIR } else { (Get-Location).Path }
$buildPath = Join-Path $projectPath $env:BUILD_PATH
$logPath = Join-Path $buildPath "unity-$($env:BUILD_TARGET).log"

if ([string]::IsNullOrWhiteSpace($env:UNITY_EXECUTABLE)) {
    $versionFile = Join-Path $projectPath 'ProjectSettings/ProjectVersion.txt'
    $version = (Select-String -Path $versionFile -Pattern '^m_EditorVersion: (.+)$').Matches[0].Groups[1].Value
    if ([string]::IsNullOrWhiteSpace($version)) {
        throw "Could not determine the Unity version from $versionFile."
    }

    $unityCandidate = Join-Path ${env:ProgramFiles} "Unity/Hub/Editor/$version/Editor/Unity.exe"
    if (-not (Test-Path -LiteralPath $unityCandidate -PathType Leaf)) {
        throw "Unity $version was not found in the standard Unity Hub location. Install it, or set UNITY_EXECUTABLE locally."
    }

    $env:UNITY_EXECUTABLE = $unityCandidate
}

New-Item -ItemType Directory -Path $buildPath -Force | Out-Null

& $env:UNITY_EXECUTABLE `
    -batchmode `
    -nographics `
    -silent-crashes `
    -quit `
    -projectPath $projectPath `
    -buildTarget $env:BUILD_TARGET `
    -executeMethod GitLabBuild.Build `
    -logFile $logPath

if ($LASTEXITCODE -ne 0) {
    throw "Unity build failed with exit code $LASTEXITCODE. See $logPath."
}
