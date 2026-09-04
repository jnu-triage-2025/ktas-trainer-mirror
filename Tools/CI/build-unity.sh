#!/usr/bin/env bash

set -euo pipefail

: "${BUILD_TARGET:?BUILD_TARGET must be set to a Unity BuildTarget}"
: "${BUILD_NAME:=ktas-trainer}"
: "${BUILD_PATH:=Build}"
# 'Player' builds the normal client; 'Server' builds the dedicated headless server.
: "${BUILD_SUBTARGET:=Player}"
: "${KEEP_BUILD_OUTPUT:=0}"
: "${PRUNE_BUILD_OUTPUT:=0}"

case "$(printf '%s' "${BUILD_SUBTARGET}" | tr '[:upper:]' '[:lower:]')" in
  player) BUILD_SUBTARGET='Player' ;;
  server) BUILD_SUBTARGET='Server' ;;
  *)
    echo "BUILD_SUBTARGET must be 'Player' or 'Server', but was '${BUILD_SUBTARGET}'." >&2
    exit 1
    ;;
esac

project_path="${CI_PROJECT_DIR:-$(pwd)}"

find_unity_executable() {
  if [[ -n "${UNITY_EXECUTABLE:-}" ]]; then
    printf '%s\n' "${UNITY_EXECUTABLE}"
    return
  fi

  local version_file="${project_path}/ProjectSettings/ProjectVersion.txt"
  local version
  version="$(sed -n 's/^m_EditorVersion: //p' "${version_file}" | head -n 1)"
  if [[ -z "${version}" ]]; then
    echo "Could not determine the Unity version from ${version_file}." >&2
    exit 1
  fi

  local candidate="/Applications/Unity/Hub/Editor/${version}/Unity.app/Contents/MacOS/Unity"
  if [[ -x "${candidate}" ]]; then
    printf '%s\n' "${candidate}"
    return
  fi

  echo "Unity ${version} was not found in the standard Unity Hub location." >&2
  echo "Install that Editor version and its required build-support module, or set UNITY_EXECUTABLE locally." >&2
  exit 1
}

unity_executable="$(find_unity_executable)"

build_directory="${project_path}/${BUILD_PATH}"
case "${build_directory}" in
  "${project_path}"/*) ;;
  *)
    echo "BUILD_PATH must resolve inside the Unity project directory." >&2
    exit 1
    ;;
esac

build_identifier="$(git -C "${project_path}" tag --points-at HEAD --sort=refname | head -n 1)"
if [[ -z "${build_identifier}" ]]; then
  build_identifier="$(git -C "${project_path}" rev-parse --short=7 HEAD)"
fi

if [[ -n "${UNITY_LOG_ARTIFACT_PATH:-}" ]]; then
  log_path="${project_path}/${UNITY_LOG_ARTIFACT_PATH}"
else
  log_path="${build_directory}/unity-${BUILD_TARGET}-${BUILD_SUBTARGET}.log"
fi
mkdir -p "${build_directory}" "$(dirname "${log_path}")"

# CI agents discard the player to keep the workspace small. Set
# KEEP_BUILD_OUTPUT=1 when the build output itself is the deliverable,
# which is the usual case for a dedicated server build.
case "$(printf '%s' "${KEEP_BUILD_OUTPUT}" | tr '[:upper:]' '[:lower:]')" in
  1 | true | yes) ;;
  *) trap 'rm -rf -- "${build_directory}"' EXIT ;;
esac

"${unity_executable}" \
  -batchmode \
  -nographics \
  -silent-crashes \
  -quit \
  -projectPath "${project_path}" \
  -buildTarget "${BUILD_TARGET}" \
  -standaloneBuildSubtarget "${BUILD_SUBTARGET}" \
  -executeMethod GitLabBuild.Build \
  -logFile "${log_path}"

case "$(printf '%s' "${PRUNE_BUILD_OUTPUT}" | tr '[:upper:]' '[:lower:]')" in
  1 | true | yes)
    find "${build_directory}" -mindepth 1 -maxdepth 1 -type d -name 'Build-*' \
      ! -name "Build-${build_identifier}" -exec rm -rf -- {} +
    ;;
esac
