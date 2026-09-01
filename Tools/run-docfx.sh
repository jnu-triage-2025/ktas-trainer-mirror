#!/usr/bin/env bash
# Runs the repository-local DocFX runtime on macOS and Linux.
set -euo pipefail

SCRIPT_DIRECTORY="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DOCFX_VERSION="2.78.5"
DOTNET_ROOT_DIRECTORY="${SCRIPT_DIRECTORY}/.dotnet-sdk"
DOTNET_EXECUTABLE="${DOTNET_ROOT_DIRECTORY}/dotnet"
DOCFX_DLL="${SCRIPT_DIRECTORY}/.dotnet-tools/.store/docfx/${DOCFX_VERSION}/docfx/${DOCFX_VERSION}/tools/net8.0/any/docfx.dll"
DOCFX_CLI_HOME="${SCRIPT_DIRECTORY}/.docfx-cli-home"

if [[ ! -x "${DOTNET_EXECUTABLE}" || ! -f "${DOCFX_DLL}" ]]; then
  echo "DocFX ${DOCFX_VERSION} is not installed at ${SCRIPT_DIRECTORY}/.dotnet-tools." >&2
  echo "Run Tools/setup-docfx.sh to install the repository-local runtime." >&2
  exit 1
fi

export DOTNET_ROOT="${DOTNET_ROOT_DIRECTORY}"
export DOTNET_ROOT_ARM64="${DOTNET_ROOT_DIRECTORY}"
export DOTNET_CLI_HOME="${DOCFX_CLI_HOME}"
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
exec "${DOTNET_EXECUTABLE}" exec "${DOCFX_DLL}" "$@"
