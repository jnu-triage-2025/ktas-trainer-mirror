[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($env:BUILD_TARGET)) {
    throw 'BUILD_TARGET must be set to a Unity BuildTarget.'
}

# 'Player' builds the normal client; 'Server' builds the dedicated headless server.
if ([string]::IsNullOrWhiteSpace($env:BUILD_SUBTARGET)) {
    $env:BUILD_SUBTARGET = 'Player'
}

switch ($env:BUILD_SUBTARGET.Trim().ToLowerInvariant()) {
    'player' { $env:BUILD_SUBTARGET = 'Player' }
    'server' { $env:BUILD_SUBTARGET = 'Server' }
    default { throw "BUILD_SUBTARGET must be 'Player' or 'Server', but was '$($env:BUILD_SUBTARGET)'." }
}

if ([string]::IsNullOrWhiteSpace($env:BUILD_NAME)) {
    $env:BUILD_NAME = 'ktas-trainer'
}

if ([string]::IsNullOrWhiteSpace($env:BUILD_PATH)) {
    $env:BUILD_PATH = 'Build'
}

$projectPath = if ($env:CI_PROJECT_DIR) { $env:CI_PROJECT_DIR } else { (Get-Location).Path }
$projectPath = [IO.Path]::GetFullPath($projectPath)
$buildPath = [IO.Path]::GetFullPath((Join-Path $projectPath $env:BUILD_PATH))
$projectPathPrefix = $projectPath.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not $buildPath.StartsWith($projectPathPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'BUILD_PATH must resolve inside the Unity project directory.'
}

$logArtifactPath = if ($env:UNITY_LOG_ARTIFACT_PATH) {
    [IO.Path]::GetFullPath((Join-Path $projectPath $env:UNITY_LOG_ARTIFACT_PATH))
} else {
    $null
}
$logPath = if ($logArtifactPath) {
    $logArtifactPath
} else {
    Join-Path $buildPath "unity-$($env:BUILD_TARGET)-$($env:BUILD_SUBTARGET).log"
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

New-Item -ItemType Directory -Path $buildPath -Force | Out-Null
New-Item -ItemType Directory -Path (Split-Path -Parent $logPath) -Force | Out-Null

try {
    & $env:UNITY_EXECUTABLE `
        -batchmode `
        -nographics `
        -silent-crashes `
        -quit `
        -projectPath $projectPath `
        -buildTarget $env:BUILD_TARGET `
        -standaloneBuildSubtarget $env:BUILD_SUBTARGET `
        -executeMethod GitLabBuild.Build `
        -logFile $logPath

    $unityExitCode = $LASTEXITCODE
    if ($null -ne $unityExitCode -and $unityExitCode -ne 0) {
        throw "Unity build failed with exit code $unityExitCode. See $logPath."
    }
}
finally {
    if ($logArtifactPath -and -not (Test-Path -LiteralPath $logPath -PathType Leaf)) {
        Set-Content -LiteralPath $logPath -Value 'Unity did not create a log file.'
    }

    # CI agents discard the player to keep the workspace small. Set
    # KEEP_BUILD_OUTPUT=1 when the build output itself is the deliverable,
    # which is the usual case for a dedicated server build.
    $keepBuildOutputValue = if ($env:KEEP_BUILD_OUTPUT) { $env:KEEP_BUILD_OUTPUT.Trim().ToLowerInvariant() } else { '' }
    $keepBuildOutput = @('1', 'true', 'yes') -contains $keepBuildOutputValue
    if (-not $keepBuildOutput -and (Test-Path -LiteralPath $buildPath)) {
        Remove-Item -LiteralPath $buildPath -Recurse -Force
    }
}
