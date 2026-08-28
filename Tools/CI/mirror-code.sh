#!/usr/bin/env bash
set -euo pipefail

project_path="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$project_path"

# Jobs that share this workspace can leave the repository shallow. Mirroring a
# shallow repository would silently publish a truncated history, so restore the
# full history first.
if [ -f "$(git rev-parse --git-dir)/shallow" ]; then
  echo 'The repository is shallow; restoring the full history before mirroring.'
  git fetch --unshallow origin
fi

git fetch origin '+refs/heads/*:refs/remotes/origin/*' --tags
python3 Tools/code-mirror/code_mirror.py --config Tools/code-mirror/code-mirror.toml --generate-config

# Republishing rewritten history is opt-in, because it overwrites the branches
# already published on the destination. The caller sets CODE_MIRROR_REBUILD only
# after a filtering change makes the recorded destination refs obsolete.
if [ "${CODE_MIRROR_REBUILD:-}" = "true" ]; then
  python3 Tools/code-mirror/code_mirror.py --config Tools/code-mirror/code-mirror.toml --dry-run --rebuild
  python3 Tools/code-mirror/code_mirror.py --config Tools/code-mirror/code-mirror.toml --rebuild --reset-destination --push
else
  python3 Tools/code-mirror/code_mirror.py --config Tools/code-mirror/code-mirror.toml --dry-run
  python3 Tools/code-mirror/code_mirror.py --config Tools/code-mirror/code-mirror.toml --push
fi
