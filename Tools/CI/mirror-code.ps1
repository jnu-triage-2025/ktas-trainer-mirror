Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$projectPath = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
Set-Location $projectPath

git fetch origin '+refs/heads/*:refs/remotes/origin/*' --tags
if ($LASTEXITCODE -ne 0) {
    throw 'Fetching origin branches and tags for the code mirror failed.'
}

$env:Path = [System.Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [System.Environment]::GetEnvironmentVariable('Path', 'User')

$python = Get-Command python -ErrorAction SilentlyContinue
$pythonArguments = @()
if ($null -eq $python) {
    $python = Get-Command py -ErrorAction SilentlyContinue
    $pythonArguments = @('-3')
}
if ($null -eq $python) {
    $python = Get-Command python -ErrorAction SilentlyContinue
}
if ($null -eq $python) {
    throw 'Python is required to create and push the code mirror.'
}

& $python.Source @pythonArguments 'Tools/code-mirror/code_mirror.py' '--config' 'Tools/code-mirror/code-mirror.toml' '--generate-config'
if ($LASTEXITCODE -ne 0) {
    throw 'Generating the code-mirror configuration failed.'
}

# Republishing rewritten history is opt-in, because it overwrites the branches
# already published on the destination. The caller sets CODE_MIRROR_REBUILD only
# after a filtering change makes the recorded destination refs obsolete.
$reviewArguments = @()
$publishArguments = @()
if ($env:CODE_MIRROR_REBUILD -eq 'true') {
    $reviewArguments = @('--rebuild')
    $publishArguments = @('--rebuild', '--reset-destination')
}

& $python.Source @pythonArguments 'Tools/code-mirror/code_mirror.py' '--config' 'Tools/code-mirror/code-mirror.toml' '--dry-run' @reviewArguments
if ($LASTEXITCODE -ne 0) {
    throw 'Validating the code-mirror result failed.'
}

& $python.Source @pythonArguments 'Tools/code-mirror/code_mirror.py' '--config' 'Tools/code-mirror/code-mirror.toml' '--push' @publishArguments
if ($LASTEXITCODE -ne 0) {
    throw 'Pushing the code mirror failed.'
}
