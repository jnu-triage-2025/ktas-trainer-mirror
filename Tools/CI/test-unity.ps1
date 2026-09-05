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

$referenceLogPath = if ($env:UNITY_REFERENCE_LOG_PATH) {
    [IO.Path]::GetFullPath((Join-Path $projectPath $env:UNITY_REFERENCE_LOG_PATH))
} else {
    Join-Path $projectPath 'artifacts/unity-reference-validation.log'
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

# Remove only the generated result so a failed preflight cannot publish a cached test run.
if (Test-Path -LiteralPath $resultsPath -PathType Leaf) {
    Remove-Item -LiteralPath $resultsPath -Force
}

# Use the same build-scoped validator as the editor and player build callbacks.
New-Item -ItemType Directory -Path (Split-Path -Parent $referenceLogPath) -Force | Out-Null
Write-Host 'Validating serialized references for enabled build scenes and runtime dependencies.'
# Unity.exe is a GUI application. Piping its output makes Windows PowerShell wait
# for termination before reading LASTEXITCODE, even when Unity writes to a log file.
& $env:UNITY_EXECUTABLE `
    -batchmode `
    -nographics `
    -silent-crashes `
    -quit `
    -projectPath $projectPath `
    -executeMethod TriageTrainer.Editor.SerializedReferenceBuildValidator.ValidateProject `
    -logFile $referenceLogPath | Out-Host

$validationExitCode = $LASTEXITCODE
if ($null -eq $validationExitCode -or $validationExitCode -ne 0) {
    throw "Serialized reference validation failed with exit code $validationExitCode. See $referenceLogPath."
}

# Test Runner owns termination; do not exit before asynchronous tests complete.
& $env:UNITY_EXECUTABLE `
    -batchmode `
    -nographics `
    -silent-crashes `
    -projectPath $projectPath `
    -runTests `
    -testPlatform $testPlatform `
    -testResults $resultsPath `
    -logFile $logPath | Out-Host

$unityExitCode = $LASTEXITCODE
if ($null -eq $unityExitCode -or $unityExitCode -ne 0) {
    throw "Unity tests failed with exit code $unityExitCode. See $logPath."
}
