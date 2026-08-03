#!/bin/bash
# Builds a single specified mod folder (e.g., sample_mods/KamikazeGambit) and writes
# a self-contained, ready-to-drop-in folder into <repo>/Mods/<ModName>/.
# Optionally also copies it into the live game's Mods/ directory with --install.
#
# Usage:
#   ./build_single.sh sample_mods/KamikazeGambit [--install]
#   GAMBONANZA_DIR=/path ./build_single.sh sample_mods/KamikazeGambit --install

set -euo pipefail

if [ "$#" -lt 1 ]; then
    echo "Usage: $0 <path_to_mod_folder> [--install]" >&2
    echo "Example: $0 sample_mods/KamikazeGambit --install" >&2
    exit 1
fi

TARGET_MOD_DIR="$1"
shift

INSTALL=0
[ "${1:-}" = "--install" ] && INSTALL=1

# Resolve full paths
MOD_SRC_DIR="$(cd "$TARGET_MOD_DIR" && pwd)"
SAMPLES_DIR="$(dirname "$MOD_SRC_DIR")"
REPO_DIR="$(cd "$SAMPLES_DIR/.." && pwd)"
DIST_DIR="$REPO_DIR/Mods"

if [ ! -f "$MOD_SRC_DIR/mod.json" ]; then
    echo "Error: Directory $MOD_SRC_DIR does not contain a valid mod.json" >&2
    exit 1
fi

if [ "$INSTALL" -eq 1 ]; then
    normalize_path() {
        local p="$1"
        case "$p" in
            [A-Za-z]:\\*)
                local drive="${p:0:1}"
                p="/${drive,,}/${p:3}"
                p="${p//\\//}"
                ;;
            [A-Za-z]:/*)
                local drive="${p:0:1}"
                p="/${drive,,}/${p:3}"
                ;;
        esac
        printf '%s\n' "$p"
    }

    find_game_dir() {
        if [ -n "${GAMBONANZA_DIR:-}" ]; then
            local normalized
            normalized="$(normalize_path "$GAMBONANZA_DIR")"
            [ -d "$normalized" ] || { echo "GAMBONANZA_DIR does not exist: $GAMBONANZA_DIR (normalized: $normalized)" >&2; return 1; }
            printf '%s\n' "$normalized"; return
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

    find_managed_dir() {
        local game="$1"
        local candidates=(
            "Gambonanza.app/Contents/Resources/Data/Managed"
            "Gambonanza_Data/Managed"
            "Gambonanza/Gambonanza_Data/Managed"
        )
        for sub in "${candidates[@]}"; do
            [ -d "$game/$sub" ] && { printf '%s\n' "$game/$sub"; return; }
        done
        echo "Could not find a Managed/ directory under $game." >&2
        return 1
    }

    derive_mods_dir() {
        local game="$1"
        local managed="$2"
        local data_dir runtime_dir
        data_dir="$(dirname "$managed")"
        if [ "$(basename "$data_dir")" = "Gambonanza_Data" ]; then
            runtime_dir="$(dirname "$data_dir")"
            printf '%s\n' "$runtime_dir/Mods"
        else
            printf '%s\n' "$game/Mods"
        fi
    }

    GAME_DIR="$(find_game_dir)"
    MANAGED_DIR="$(find_managed_dir "$GAME_DIR")"
    LIVE_MODS_DIR="$(derive_mods_dir "$GAME_DIR" "$MANAGED_DIR")"
    echo "==> Will install into: $LIVE_MODS_DIR"
fi

find_project_file() {
    local src="$1"
    local csproj
    csproj="$(command find "$src" -maxdepth 1 -name '*.csproj' -print | sort | head -n 1)"
    if [ -n "$csproj" ]; then printf '%s\n' "$csproj"; fi
    return 0
}

assembly_name_for() {
    local src="$1"
    local csproj asm
    csproj="$(find_project_file "$src")"
    [ -n "$csproj" ] || return 1
    asm="$(sed -n 's:.*<AssemblyName>\(.*\)</AssemblyName>.*:\1:p' "$csproj" | head -n 1)"
    if [ -n "$asm" ]; then printf '%s\n' "$asm"; else basename "${csproj%.csproj}"; fi
}

copy_extra_assets() {
    local src="$1"
    local out="$2"
    command find "$src" -maxdepth 1 -type f \
        ! -name 'mod.json' \
        ! -name '*.csproj' \
        ! -name '*.cs' \
        -print0 | while IFS= read -r -d '' asset; do
            cp "$asset" "$out/"
        done
}

mkdir -p "$DIST_DIR"

csproj="$(find_project_file "$MOD_SRC_DIR")"
if [ -z "$csproj" ]; then
    echo "Error: No .csproj file found at top level of $MOD_SRC_DIR" >&2
    exit 1
fi

mod="$(basename "$MOD_SRC_DIR")"
asm="$(assembly_name_for "$MOD_SRC_DIR")"
out="$DIST_DIR/$mod"

echo "==> Building single mod: $mod"
dotnet build "$csproj" -c Release --nologo -v minimal

dll="$MOD_SRC_DIR/bin/Release/$asm.dll"
if [ ! -f "$dll" ]; then
    dll="$(command find "$MOD_SRC_DIR/bin/Release" -name "$asm.dll" -print | head -n 1)"
fi
[ -f "$dll" ] || { echo "Error: Missing build output for $mod (expected $asm.dll under $MOD_SRC_DIR/bin/Release)" >&2; exit 1; }

rm -rf "$out"
mkdir -p "$out"
cp "$dll" "$out/"
cp "$MOD_SRC_DIR/mod.json" "$out/"
copy_extra_assets "$MOD_SRC_DIR" "$out"
echo "  staged -> $out"

if [ "$INSTALL" -eq 1 ]; then
    live="$LIVE_MODS_DIR/$mod"
    rm -rf "$live"
    mkdir -p "$live"
    cp -R "$out/." "$live/"
    echo "  installed -> $live"
fi

echo
if [ "$INSTALL" -eq 1 ]; then
    echo "Mod '$mod' built and installed into $LIVE_MODS_DIR/$mod."
    echo "Launch the game from Steam to pick it up."
else
    echo "Mod '$mod' built successfully. Staged output is in $DIST_DIR/$mod."
    echo "To install into the live game, re-run with --install appended."
fi