[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$projectPath = if ($env:CI_PROJECT_DIR) { $env:CI_PROJECT_DIR } else { (Get-Location).Path }
$projectPath = [IO.Path]::GetFullPath($projectPath)
$testPlatform = if ($env:TEST_PLATFORM) { $env:TEST_PLATFORM } else { 'editmode' }
$resultsPath = if ($env:UNITY_TEST_RESULTS_PATH) {
    [IO.Path]::GetFullPath((Join-Path $projectPath $env:UNITY_TEST_RESULTS_PATH))
} else {
    Join-Path $projectPath "artifacts/unity-$testPlatform-results.xml"
}
$logPath = if ($env:UNITY_TEST_LOG_PATH) {
    [IO.Path]::GetFullPath((Join-Path $projectPath $env:UNITY_TEST_LOG_PATH))
} else {
    Join-Path $projectPath "artifacts/unity-$testPlatform.log"
}

if ([string]::IsNullOrWhiteSpace($env:UNITY_EXECUTABLE)) {
    $versionFile = Join-Path $projectPath 'ProjectSettings/ProjectVersion.txt'
    $version = (Select-String -Path $versionFile -Pattern '^m_EditorVersion: (.+)$').Matches[0].Groups[1].Value
    if ([string]::IsNullOrWhiteSpace($version)) {
        throw "Could not determine the Unity version from $versionFile."
    }

    $unityCandidates = @(
        (Join-Path $env:ProgramFiles "Unity/Hub/Editor/$version/Editor/Unity.exe"),
        (Join-Path ${env:ProgramFiles(x86)} "Unity/Hub/Editor/$version/Editor/Unity.exe")
    )
    $unityCandidate = $unityCandidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($unityCandidate)) {
        throw "Unity $version was not found in the standard Unity Hub location. Install it, or set UNITY_EXECUTABLE locally."
    }

    $env:UNITY_EXECUTABLE = $unityCandidate
}

New-Item -ItemType Directory -Path (Split-Path -Parent $resultsPath) -Force | Out-Null
New-Item -ItemType Directory -Path (Split-Path -Parent $logPath) -Force | Out-Null

& $env:UNITY_EXECUTABLE `
    -batchmode `
    -nographics `
    -silent-crashes `
    -quit `
    -projectPath $projectPath `
    -runTests `
    -testPlatform $testPlatform `
    -testResults $resultsPath `
    -logFile $logPath

$unityExitCode = $LASTEXITCODE
if ($null -ne $unityExitCode -and $unityExitCode -ne 0) {
    throw "Unity tests failed with exit code $unityExitCode. See $logPath."
}
