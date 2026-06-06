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

echo "==> Discovering sample mods in: $SAMPLES_DIR"
if [ "$INSTALL" -eq 1 ]; then
    echo "==> Sample mod install target: $LIVE_MODS_DIR"
else
    echo "==> Sample mod staging target: $DIST_DIR"
fi

# Known sample renames. Remove stale staged/live folders so users who update
# from an older checkout do not keep loading both the old and new mod IDs.
cleanup_legacy_mod_dirs() {
    local legacy
    for legacy in CaltropsGambit; do
        if [ -d "$DIST_DIR/$legacy" ]; then
            rm -rf "$DIST_DIR/$legacy"
            echo "  removed legacy staged mod: $DIST_DIR/$legacy"
        fi
        if [ "$INSTALL" -eq 1 ] && [ -d "$LIVE_MODS_DIR/$legacy" ]; then
            rm -rf "$LIVE_MODS_DIR/$legacy"
            echo "  removed legacy installed mod: $LIVE_MODS_DIR/$legacy"
        fi
    done
}
cleanup_legacy_mod_dirs

found=0
for src in "$SAMPLES_DIR"/*; do
    [ -d "$src" ] || continue
    [ -f "$src/mod.json" ] || continue
    csproj="$(find_project_file "$src")"
    if [ -z "$csproj" ]; then
        echo "  skip $(basename "$src"): no .csproj at top level"
        continue
    fi

    found=1
    mod="$(basename "$src")"
    asm="$(assembly_name_for "$src")"
    out="$DIST_DIR/$mod"

    echo "==> Building $mod"
    dotnet build "$csproj" -c Release --nologo -v minimal

    dll="$src/bin/Release/$asm.dll"
    if [ ! -f "$dll" ]; then
        dll="$(command find "$src/bin/Release" -name "$asm.dll" -print | head -n 1)"
    fi
    [ -f "$dll" ] || { echo "missing build output for $mod (expected $asm.dll under $src/bin/Release)" >&2; exit 1; }

    rm -rf "$out"
    mkdir -p "$out"
    cp "$dll" "$out/"
    cp "$src/mod.json" "$out/"
    copy_extra_assets "$src" "$out"
    echo "  staged -> $out"

    if [ "$INSTALL" -eq 1 ]; then
        live="$LIVE_MODS_DIR/$mod"
        rm -rf "$live"
        mkdir -p "$live"
        cp -R "$out/." "$live/"
        echo "  installed -> $live"
    fi
done

[ "$found" -eq 1 ] || { echo "No sample mods found under $SAMPLES_DIR" >&2; exit 1; }

echo
if [ "$INSTALL" -eq 1 ]; then
    installed_count="$(command find "$LIVE_MODS_DIR" -mindepth 2 -maxdepth 2 -name mod.json -print | wc -l | tr -d ' ')"
    echo "Installed sample mod manifests found: $installed_count"
    command find "$LIVE_MODS_DIR" -mindepth 2 -maxdepth 2 -name mod.json -print | sort | sed 's/^/  - /'
    echo
    echo "All sample mods built and installed into $LIVE_MODS_DIR/."
    echo "Launch the game from Steam to pick them up."
else
    echo "All sample mods built. Distributable folders are in $DIST_DIR/."
    echo "To install into the live game, re-run with --install,"
    echo "or copy each subfolder into your Gambonanza/Mods/ directory by hand."
fi
