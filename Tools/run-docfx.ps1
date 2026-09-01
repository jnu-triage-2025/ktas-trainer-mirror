[CmdletBinding()]
param(
  [Parameter(ValueFromRemainingArguments = $true)]
  [string[]]$DocfxArguments
)

$ErrorActionPreference = 'Stop'

$scriptDirectory = Split-Path -Parent $PSCommandPath
$dotnetDirectory = Join-Path $scriptDirectory '.dotnet-sdk'
$dotnetExecutable = Join-Path $dotnetDirectory 'dotnet.exe'
$docfxVersion = '2.78.5'
$docfxDll = Join-Path $scriptDirectory ".dotnet-tools/.store/docfx/$docfxVersion/docfx/$docfxVersion/tools/net8.0/any/docfx.dll"

if (-not (Test-Path -LiteralPath $dotnetExecutable -PathType Leaf) -or -not (Test-Path -LiteralPath $docfxDll -PathType Leaf)) {
  throw 'DocFX is not initialized. Run Tools/setup-docfx.ps1 first.'
}

$env:DOTNET_ROOT = $dotnetDirectory
$env:DOTNET_CLI_HOME = Join-Path $scriptDirectory '.docfx-cli-home'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
& $dotnetExecutable exec $docfxDll @DocfxArguments
exit $LASTEXITCODE
