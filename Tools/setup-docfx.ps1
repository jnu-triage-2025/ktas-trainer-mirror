[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$scriptDirectory = Split-Path -Parent $PSCommandPath
$dotnetDirectory = Join-Path $scriptDirectory '.dotnet-sdk'
$toolDirectory = Join-Path $scriptDirectory '.dotnet-tools'
$dotnetExecutable = Join-Path $dotnetDirectory 'dotnet.exe'
$dotnetVersion = '8.0.424'
$docfxVersion = '2.78.5'
$installerPath = Join-Path ([System.IO.Path]::GetTempPath()) ('dotnet-install-' + [guid]::NewGuid().ToString('N') + '.ps1')
$workingDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ('docfx-install-' + [guid]::NewGuid().ToString('N'))

try {
  if (-not (Test-Path -LiteralPath $dotnetExecutable -PathType Leaf)) {
    Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installerPath
    & $installerPath -Version $dotnetVersion -InstallDir $dotnetDirectory -NoPath
  }

  $env:DOTNET_ROOT = $dotnetDirectory
  $env:DOTNET_CLI_HOME = Join-Path $scriptDirectory '.docfx-cli-home'
  $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
  $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

  New-Item -ItemType Directory -Force -Path $toolDirectory | Out-Null
  New-Item -ItemType Directory -Force -Path $workingDirectory | Out-Null
  Push-Location $workingDirectory
  try {
    if (Test-Path -LiteralPath (Join-Path $toolDirectory 'docfx.exe') -PathType Leaf) {
      & $dotnetExecutable tool update docfx --version $docfxVersion --tool-path $toolDirectory
    } else {
      & $dotnetExecutable tool install docfx --version $docfxVersion --tool-path $toolDirectory
    }
  } finally {
    Pop-Location
  }

  & (Join-Path $scriptDirectory 'run-docfx.ps1') --version
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} finally {
  Remove-Item -LiteralPath $installerPath -Force -ErrorAction SilentlyContinue
  Remove-Item -LiteralPath $workingDirectory -Recurse -Force -ErrorAction SilentlyContinue
}
