Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$projectPath = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
Set-Location $projectPath

git fetch origin '+refs/heads/*:refs/remotes/origin/*' --tags
if ($LASTEXITCODE -ne 0) {
    throw 'Fetching origin branches and tags for the code mirror failed.'
}

$python = Get-Command python -ErrorAction SilentlyContinue
$pythonArguments = @()
if ($null -eq $python) {
    $python = Get-Command py -ErrorAction SilentlyContinue
    $pythonArguments = @('-3')
}
if ($null -eq $python) {
    throw 'Python 3 is required to create and push the code mirror.'
}

& $python.Source @pythonArguments 'Tools/code-mirror/code_mirror.py' '--config' 'Tools/code-mirror/code-mirror.toml' '--generate-config'
if ($LASTEXITCODE -ne 0) {
    throw 'Generating the code-mirror configuration failed.'
}

& $python.Source @pythonArguments 'Tools/code-mirror/code_mirror.py' '--config' 'Tools/code-mirror/code-mirror.toml' '--dry-run'
if ($LASTEXITCODE -ne 0) {
    throw 'Validating the code-mirror result failed.'
}

& $python.Source @pythonArguments 'Tools/code-mirror/code_mirror.py' '--config' 'Tools/code-mirror/code-mirror.toml' '--push'
if ($LASTEXITCODE -ne 0) {
    throw 'Pushing the code mirror failed.'
}
