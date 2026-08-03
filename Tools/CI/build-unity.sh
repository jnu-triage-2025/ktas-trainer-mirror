#!/usr/bin/env bash

set -euo pipefail

: "${BUILD_TARGET:?BUILD_TARGET must be set to a Unity BuildTarget}"
: "${BUILD_NAME:=ktas-trainer}"
: "${BUILD_PATH:=build}"

project_path="${CI_PROJECT_DIR:-$(pwd)}"
log_path="${project_path}/${BUILD_PATH}/unity-${BUILD_TARGET}.log"

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

log_path="${build_directory}/unity-${BUILD_TARGET}.log"
mkdir -p "${build_directory}"
trap 'rm -rf -- "${build_directory}"' EXIT

"${unity_executable}" \
  -batchmode \
  -nographics \
  -silent-crashes \
  -quit \
  -projectPath "${project_path}" \
  -buildTarget "${BUILD_TARGET}" \
  -executeMethod GitLabBuild.Build \
  -logFile "${log_path}"
