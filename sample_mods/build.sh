#!/bin/bash
# Builds every mod in sample_mods/ and writes a self-contained, ready-to-drop-in
# folder for each into <repo>/Mods/<ModName>/. Optionally also copies them into
# the live game's Mods/ directory with --install.
#
# Cross-platform: macOS, Linux, Windows (Git Bash / WSL). Auto-detects the
# game install. Override with GAMBONANZA_DIR.
#
#   ./build.sh                     # build + stage into <repo>/Mods/
#   ./build.sh --install           # also copy into Gambonanza/Mods/
#   GAMBONANZA_DIR=/path ./build.sh --install
#
# A mod folder is just:
#   Mods/<ModName>/
#     mod.json             metadata read by Gambonanza.ModHost
#     Gambonanza.<Mod>.dll compiled IMod, loaded with Assembly.LoadFrom
#     <any extra assets>   e.g. kamikaze.png

set -euo pipefail

SAMPLES_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_DIR="$(cd "$SAMPLES_DIR/.." && pwd)"
DIST_DIR="$REPO_DIR/Mods"

INSTALL=0
[ "${1:-}" = "--install" ] && INSTALL=1

if [ "$INSTALL" -eq 1 ]; then
    find_game_dir() {
        if [ -n "${GAMBONANZA_DIR:-}" ]; then
            [ -d "$GAMBONANZA_DIR" ] || { echo "GAMBONANZA_DIR does not exist: $GAMBONANZA_DIR" >&2; return 1; }
            printf '%s\n' "$GAMBONANZA_DIR"; return
        fi
        local candidates=(
            "$HOME/Library/Application Support/Steam/steamapps/common/Gambonanza"
            "$HOME/.local/share/Steam/steamapps/common/Gambonanza"
            "$HOME/.steam/steam/steamapps/common/Gambonanza"
            "/c/Program Files (x86)/Steam/steamapps/common/Gambonanza"
            "/c/Program Files/Steam/steamapps/common/Gambonanza"
        )
        for c in "${candidates[@]}"; do
            [ -d "$c" ] && { printf '%s\n' "$c"; return; }
        done
        echo "Could not auto-detect a Gambonanza install." >&2
        echo "Set GAMBONANZA_DIR to the install path." >&2
        return 1
    }
    GAME_DIR="$(find_game_dir)"
    LIVE_MODS_DIR="$GAME_DIR/Mods"
    echo "==> Will install into: $LIVE_MODS_DIR"
fi

# Each entry: "<source folder>:<assembly name>:<extra asset 1> <extra asset 2> ..."
MODS=(
    "SpeedMod:Gambonanza.SpeedMod:"
    "GambitApi:Gambonanza.GambitApi:"
    "KamikazeGambit:Gambonanza.KamikazeGambit:kamikaze.png"
)

mkdir -p "$DIST_DIR"

for entry in "${MODS[@]}"; do
    IFS=":" read -r mod asm assets <<<"$entry"
    src="$SAMPLES_DIR/$mod"
    out="$DIST_DIR/$mod"

    echo "==> Building $mod"
    dotnet build "$src" -c Release --nologo -v minimal

    dll="$src/bin/Release/$asm.dll"
    [ -f "$dll" ] || { echo "missing build output: $dll" >&2; exit 1; }

    rm -rf "$out"
    mkdir -p "$out"
    cp "$dll" "$out/"
    cp "$src/mod.json" "$out/"
    for a in $assets; do
        cp "$src/$a" "$out/"
    done
    echo "  staged -> $out"

    if [ "$INSTALL" -eq 1 ]; then
        live="$LIVE_MODS_DIR/$mod"
        rm -rf "$live"
        mkdir -p "$live"
        cp -R "$out/." "$live/"
        echo "  installed -> $live"
    fi
done

echo
if [ "$INSTALL" -eq 1 ]; then
    echo "All sample mods built and installed into $LIVE_MODS_DIR/."
    echo "Launch the game from Steam to pick them up."
else
    echo "All sample mods built. Distributable folders are in $DIST_DIR/."
    echo "To install into the live game, re-run with --install,"
    echo "or copy each subfolder into your Gambonanza/Mods/ directory by hand."
fi
