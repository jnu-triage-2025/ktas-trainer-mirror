#!/usr/bin/env bash

set -euo pipefail

: "${TEST_PLATFORM:=editmode}"
: "${UNITY_TEST_RESULTS_PATH:=artifacts/unity-${TEST_PLATFORM}-results.xml}"
: "${UNITY_TEST_LOG_PATH:=artifacts/unity-${TEST_PLATFORM}.log}"
: "${UNITY_REFERENCE_LOG_PATH:=artifacts/unity-reference-validation.log}"

project_path="${CI_PROJECT_DIR:-$(pwd)}"
version_file="${project_path}/ProjectSettings/ProjectVersion.txt"
version="$(sed -n 's/^m_EditorVersion: //p' "${version_file}" | head -n 1)"

if [[ -z "${version}" ]]; then
  echo "Could not determine the Unity version from ${version_file}." >&2
  exit 1
fi

if [[ -n "${UNITY_EXECUTABLE:-}" ]]; then
  unity_executable="${UNITY_EXECUTABLE}"
else
  unity_executable="/Applications/Unity/Hub/Editor/${version}/Unity.app/Contents/MacOS/Unity"
fi

if [[ ! -x "${unity_executable}" ]]; then
  echo "Unity executable was not found or is not executable: ${unity_executable}" >&2
  exit 1
fi

mkdir -p "${project_path}/$(dirname "${UNITY_TEST_RESULTS_PATH}")" "${project_path}/$(dirname "${UNITY_TEST_LOG_PATH}")"

# Remove only the generated result so a failed preflight cannot publish a cached test run.
rm -f -- "${project_path}/${UNITY_TEST_RESULTS_PATH}"

# Use the same build-scoped validator as the editor and player build callbacks.
mkdir -p "${project_path}/$(dirname "${UNITY_REFERENCE_LOG_PATH}")"
echo "Validating serialized references for enabled build scenes and runtime dependencies."
"${unity_executable}" \
  -batchmode \
  -nographics \
  -silent-crashes \
  -quit \
  -projectPath "${project_path}" \
  -executeMethod TriageTrainer.Editor.SerializedReferenceBuildValidator.ValidateProject \
  -logFile "${project_path}/${UNITY_REFERENCE_LOG_PATH}"

"${unity_executable}" \
  -batchmode \
  -nographics \
  -silent-crashes \
  -projectPath "${project_path}" \
  -runTests \
  -testPlatform "${TEST_PLATFORM}" \
  -testResults "${project_path}/${UNITY_TEST_RESULTS_PATH}" \
  -logFile "${project_path}/${UNITY_TEST_LOG_PATH}"
