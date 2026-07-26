#!/bin/bash
# Launch GambonanzaAssets. Installs the two Python packages it needs on first run.
#
#   ./gambonanza-assets.sh                          auto-detect the game, open the UI
#   ./gambonanza-assets.sh --game "/path/to/Gambonanza"
#   ./gambonanza-assets.sh --restore                put the vanilla art back, no UI
#
# Works on macOS, Linux, and Windows under Git Bash / WSL.

set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")"

PY=""
for c in python3 python py; do
    if command -v "$c" >/dev/null 2>&1 && "$c" -c 'import sys; sys.exit(0 if sys.version_info >= (3,8) else 1)' 2>/dev/null; then
        PY="$c"; break
    fi
done

if [ -z "$PY" ]; then
    echo "GambonanzaAssets needs Python 3.8 or newer."
    echo "Install it from https://www.python.org/downloads/ and run this again."
    exit 1
fi

if ! "$PY" -c 'import UnityPy, PIL' >/dev/null 2>&1; then
    echo "==> First run: installing UnityPy and Pillow (one time, ~20s)"
    "$PY" -m pip install --quiet --user --upgrade UnityPy Pillow \
        || "$PY" -m pip install --quiet --upgrade UnityPy Pillow
fi

exec "$PY" gambonanza-assets.py "$@"
