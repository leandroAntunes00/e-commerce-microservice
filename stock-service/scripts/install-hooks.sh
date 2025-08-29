#!/usr/bin/env bash
set -euo pipefail

GIT_DIR=$(git rev-parse --git-dir 2>/dev/null || echo ".git")
HOOKS_DIR="$GIT_DIR/hooks"
mkdir -p "$HOOKS_DIR"
cp -f "$(pwd)/scripts/pre-commit" "$HOOKS_DIR/pre-commit"
chmod +x "$HOOKS_DIR/pre-commit"
echo "pre-commit hook instalado em $HOOKS_DIR/pre-commit"
