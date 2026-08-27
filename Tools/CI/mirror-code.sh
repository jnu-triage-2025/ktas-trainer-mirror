#!/usr/bin/env bash
set -euo pipefail

project_path="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$project_path"

git fetch origin '+refs/heads/*:refs/remotes/origin/*' --tags
python3 Tools/code-mirror/code_mirror.py --config Tools/code-mirror/code-mirror.toml --generate-config
python3 Tools/code-mirror/code_mirror.py --config Tools/code-mirror/code-mirror.toml --dry-run
python3 Tools/code-mirror/code_mirror.py --config Tools/code-mirror/code-mirror.toml --push
