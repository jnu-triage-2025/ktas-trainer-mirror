#!/usr/bin/env bash

set -euo pipefail

solution_path="${CSHARP_SOLUTION_PATH:-ktas-trainer.sln}"

if [[ ! -f "${solution_path}" ]]; then
  echo "C# solution was not found: ${solution_path}" >&2
  exit 1
fi

dotnet format "${solution_path}" \
  --include "Assets/Modules/MultiplayerInfrastructure" \
  --include "Assets/Modules/TriageTrainer" \
  --exclude "Assets/Modules/FishNet" \
  --exclude "Assets/Modules/ParrelSync" \
  --exclude "Assets/Packages" \
  --severity warn \
  "$@"
