#!/usr/bin/env bash

set -euo pipefail

git config core.hooksPath .githooks
echo "C# pre-commit hook is enabled through .githooks."
