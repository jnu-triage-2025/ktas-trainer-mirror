#!/usr/bin/env bash

set -euo pipefail

solution_path="${CSHARP_SOLUTION_PATH:-ktas-trainer.sln}"

if [[ ! -f "${solution_path}" ]]; then
  echo "C# solution was not found: ${solution_path}" >&2
  exit 1
fi

dotnet_command="$(command -v dotnet 2>/dev/null || true)"
if [[ -z "${dotnet_command}" ]]; then
  for candidate in /opt/homebrew/bin/dotnet /usr/local/bin/dotnet; do
    if [[ -x "${candidate}" ]]; then
      dotnet_command="${candidate}"
      break
    fi
  done
fi

if [[ -z "${dotnet_command}" ]]; then
  echo "dotnet SDK was not found. Install the .NET SDK or add dotnet to PATH." >&2
  exit 1
fi

"${dotnet_command}" format "${solution_path}" \
  --include "Assets/Modules/MultiplayerInfrastructure" \
  --include "Assets/Modules/TriageTrainer" \
  --exclude "Assets/Modules/FishNet" \
  --exclude "Assets/Modules/ParrelSync" \
  --exclude "Assets/Packages" \
  --severity warn \
  "$@"
