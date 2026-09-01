#!/usr/bin/env bash
# Markdown link target checker for this repository.
#
# Usage (run from Tools as working directory):
#   ./check-markdown-links.sh
#   ./check-markdown-links.sh ../Documents
#   ./check-markdown-links.sh ../Documents/README.md ../Documents/working-guide
#
# What it checks:
# - Markdown inline links in the given .md file(s) and directory trees.
# - Whether each link target path exists on disk.
#
# Notes:
# - HTTP(S), mailto, and pure anchor links are ignored.
# - Fragment identifiers (#section) are ignored for file existence checks.
# - Root-style links like /Agents/Templates are resolved from repository root.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

if [[ "$(basename "$PWD")" != "Tools" ]]; then
  echo "WARN: current working directory is not Tools. Running anyway." >&2
fi

declare -a TARGETS
if [[ "$#" -eq 0 ]]; then
  TARGETS=("${REPO_ROOT}/Documents")
else
  for arg in "$@"; do
    if [[ "$arg" = /* ]]; then
      TARGETS+=("$arg")
    else
      TARGETS+=("${PWD}/$arg")
    fi
  done
fi

collect_markdown_files() {
  local p
  for p in "$@"; do
    if [[ -f "$p" ]]; then
      if [[ "$p" == *.md ]]; then
        echo "$p"
      fi
    elif [[ -d "$p" ]]; then
      find "$p" -type f -name "*.md"
    else
      echo "WARN: target does not exist: $p" >&2
    fi
  done
}

is_external_or_anchor() {
  local link="$1"
  [[ -z "$link" ]] && return 0
  [[ "$link" == \#* ]] && return 0
  [[ "$link" =~ ^[a-zA-Z][a-zA-Z0-9+.-]*: ]] && return 0
  [[ "$link" == //* ]] && return 0
  return 1
}

resolve_target_path() {
  local source_file="$1"
  local raw_link="$2"
  local link_no_fragment="${raw_link%%#*}"

  if [[ "$link_no_fragment" == /* ]]; then
    printf "%s%s\n" "$REPO_ROOT" "$link_no_fragment"
  else
    printf "%s/%s\n" "$(cd "$(dirname "$source_file")" && pwd)" "$link_no_fragment"
  fi
}

missing_count=0
checked_links=0

while IFS= read -r md_file; do
  while IFS= read -r match; do
    link="$(echo "$match" | sed -E 's/^.*\]\((.*)\)$/\1/')"

    # Drop optional title part: path "title"
    link="${link%% \"*}"

    # DocFX escapes Markdown-sensitive characters in generated API filenames
    # and anchors (for example, CommandDefinition\_Character.md). Normalize
    # them before resolving the target on disk.
    link="${link//\\_/_}"
    link="${link//\\#/#}"
    link="${link//\\-/-}"

    if is_external_or_anchor "$link"; then
      continue
    fi

    resolved="$(resolve_target_path "$md_file" "$link")"
    checked_links=$((checked_links + 1))

    if [[ ! -e "$resolved" ]]; then
      missing_count=$((missing_count + 1))
      echo "MISSING: ${md_file#$REPO_ROOT/} -> $link"
    fi
  done < <(grep -oE '\[[^]]+\]\([^)]*\)' "$md_file" || true)
done < <(collect_markdown_files "${TARGETS[@]}" | sort -u)

echo "Checked link targets: $checked_links"
if [[ "$missing_count" -eq 0 ]]; then
  echo "Result: OK"
  exit 0
fi

echo "Result: FAIL (missing targets: $missing_count)"
exit 1
