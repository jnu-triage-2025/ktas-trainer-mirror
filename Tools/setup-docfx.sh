#!/usr/bin/env bash
# Installs the pinned DocFX runtime for macOS and Linux into Tools/.
set -euo pipefail

SCRIPT_DIRECTORY="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DOTNET_DIRECTORY="${SCRIPT_DIRECTORY}/.dotnet-sdk"
TOOL_DIRECTORY="${SCRIPT_DIRECTORY}/.dotnet-tools"
DOTNET_VERSION="8.0.424"
DOCFX_VERSION="2.78.5"
INSTALLER="$(mktemp "${TMPDIR:-/tmp}/dotnet-install.XXXXXX")"
WORKING_DIRECTORY="$(mktemp -d "${TMPDIR:-/tmp}/docfx-install.XXXXXX")"

cleanup() {
  rm -f "${INSTALLER}"
  rm -rf "${WORKING_DIRECTORY}"
}
trap cleanup EXIT

if [[ ! -x "${DOTNET_DIRECTORY}/dotnet" ]]; then
  curl --fail --location --silent --show-error https://dot.net/v1/dotnet-install.sh --output "${INSTALLER}"
  bash "${INSTALLER}" --version "${DOTNET_VERSION}" --install-dir "${DOTNET_DIRECTORY}" --no-path
fi

export DOTNET_ROOT="${DOTNET_DIRECTORY}"
export DOTNET_ROOT_ARM64="${DOTNET_DIRECTORY}"
export DOTNET_CLI_HOME="${SCRIPT_DIRECTORY}/.docfx-cli-home"
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1

mkdir -p "${TOOL_DIRECTORY}"
pushd "${WORKING_DIRECTORY}" >/dev/null
if [[ -x "${TOOL_DIRECTORY}/docfx" ]]; then
  "${DOTNET_DIRECTORY}/dotnet" tool update docfx --version "${DOCFX_VERSION}" --tool-path "${TOOL_DIRECTORY}"
else
  "${DOTNET_DIRECTORY}/dotnet" tool install docfx --version "${DOCFX_VERSION}" --tool-path "${TOOL_DIRECTORY}"
fi
popd >/dev/null

"${SCRIPT_DIRECTORY}/run-docfx.sh" --version
