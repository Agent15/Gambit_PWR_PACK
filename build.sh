#!/bin/bash
# One-shot installer. Does everything end-to-end:
#
#   1. Auto-detects the game install (override with GAMBONANZA_DIR or arg).
#   2. Hydrates refs/ from the game's own Managed/ folder so the projects
#      can compile (those DLLs are copyrighted and shipped with the game,
#      so we never commit them to the repo — we copy from your own install).
#   3. Builds the framework (ModSdk, ModHost, GameUI, Patcher).
#   4. Patches Assembly-CSharp.dll and installs the framework DLLs into
#      Managed/. Idempotent — always patches from the .orig backup.
#   5. Builds every sample mod under sample_mods/ and stages a clean
#      drop-in folder for each into <repo>/Mods/<ModName>/.
#   6. Copies the staged sample mod folders into the live game's Mods/.
#
# Cross-platform: works on macOS, Linux, and Windows under Git Bash / WSL.
#
# Usage:
#     ./build.sh                       # full install
#     ./build.sh --skip-samples        # framework only, leaves Mods/ empty
#     ./build.sh "/path/to/Gambonanza" # explicit install path
#     GAMBONANZA_DIR="/path" ./build.sh
#
# Requires: bash + dotnet SDK (>= 8.0).

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REFS_DIR="$SCRIPT_DIR/refs"
FRAMEWORK_VERSION="$(cat "$SCRIPT_DIR/VERSION" 2>/dev/null || printf '1.0.0')"

SKIP_SAMPLES=0
GAME_ARG=""
for arg in "$@"; do
    case "$arg" in
        --skip-samples) SKIP_SAMPLES=1 ;;
        -h|--help)
            sed -n '2,/^set -/p' "$0" | sed 's/^# \?//;/^set -/d'
            exit 0
            ;;
        *) GAME_ARG="$arg" ;;
    esac
done

# ----------------------------------------------------------------------------
# 1. Locate the game install
# ----------------------------------------------------------------------------

normalize_path() {
    # Git Bash/MSYS accepts /d/foo reliably for shell file operations. A raw
    # Windows path like D:\SteamLibrary may be interpreted inconsistently by
    # bash tools even though native .NET tools can use it, causing the framework
    # to patch the real game while sample mods get copied elsewhere.
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
        [ -d "$normalized" ] || { echo "GAMBONANZA_DIR is set but does not exist: $GAMBONANZA_DIR (normalized: $normalized)" >&2; return 1; }
        printf '%s\n' "$normalized"
        return
    fi
    if [ -n "$GAME_ARG" ]; then
        local normalized
        normalized="$(normalize_path "$GAME_ARG")"
        if [ -d "$normalized" ]; then
            printf '%s\n' "$normalized"
            return
        fi
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
    echo "Pass the install path as an argument or set GAMBONANZA_DIR." >&2
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
    echo "Tried: ${candidates[*]}" >&2
    return 1
}

GAME_DIR="$(find_game_dir)"
MANAGED_DIR="$(find_managed_dir "$GAME_DIR")"
MODS_DIR="$GAME_DIR/Mods"

echo "==> Game install:  $GAME_DIR"
echo "==> Managed/ dir:  $MANAGED_DIR"

# ----------------------------------------------------------------------------
# 2. Hydrate refs/ from the user's own Managed/ folder
# ----------------------------------------------------------------------------

# Every DLL the framework + sample mods reference at compile time. The user
# already has all of these on disk inside their own game install, so we copy
# them in rather than committing them to the repo (they are copyrighted by
# Unity / Blukulele and we have no right to redistribute them).
REQUIRED_REFS=(
    Assembly-CSharp-firstpass.dll
    DOTween.dll
    Unity.TextMeshPro.dll
    UnityEngine.dll
    UnityEngine.AnimationModule.dll
    UnityEngine.AudioModule.dll
    UnityEngine.CoreModule.dll
    UnityEngine.IMGUIModule.dll
    UnityEngine.ImageConversionModule.dll
    UnityEngine.InputLegacyModule.dll
    UnityEngine.JSONSerializeModule.dll
    UnityEngine.ParticleSystemModule.dll
    UnityEngine.Physics2DModule.dll
    UnityEngine.SpriteMaskModule.dll
    UnityEngine.TextCoreTextEngineModule.dll
    UnityEngine.TextRenderingModule.dll
    UnityEngine.UI.dll
    UnityEngine.UIModule.dll
)

echo "==> Hydrating refs/ from $MANAGED_DIR"
mkdir -p "$REFS_DIR"

# Pick whichever Assembly-CSharp.dll is currently vanilla so mods compile against
# the live game's API surface. The patcher tags its output with a marker type
# (__GambonanzaModHostPatched); we grep for it as a literal string in the binary.
#   - .dll WITHOUT marker = vanilla (first install OR Steam just shipped an update
#     that overwrote our patched DLL). Use it; .orig is potentially stale.
#   - .dll WITH marker = our patched output. Use .orig (which the patcher
#     guarantees is the matching vanilla snapshot).
ASMCSHARP="$MANAGED_DIR/Assembly-CSharp.dll"
ASMCSHARP_ORIG="$MANAGED_DIR/Assembly-CSharp.dll.orig"
MARKER="__GambonanzaModHostPatched"
if grep -q "$MARKER" "$ASMCSHARP" 2>/dev/null; then
    if [ -f "$ASMCSHARP_ORIG" ]; then
        cp "$ASMCSHARP_ORIG" "$REFS_DIR/Assembly-CSharp.dll"
    else
        echo "  warn: $ASMCSHARP is patched but no .orig backup found. Using patched dll." >&2
        cp "$ASMCSHARP" "$REFS_DIR/Assembly-CSharp.dll"
    fi
else
    cp "$ASMCSHARP" "$REFS_DIR/Assembly-CSharp.dll"
fi

missing=()
for dll in "${REQUIRED_REFS[@]}"; do
    if [ -f "$MANAGED_DIR/$dll" ]; then
        cp "$MANAGED_DIR/$dll" "$REFS_DIR/$dll"
    else
        missing+=("$dll")
    fi
done

if [ "${#missing[@]}" -gt 0 ]; then
    echo "  refs/ hydration failed — these DLLs are not in $MANAGED_DIR:" >&2
    printf '    - %s\n' "${missing[@]}" >&2
    echo "  Has Steam fully installed the game?" >&2
    exit 1
fi
echo "  ok ($((${#REQUIRED_REFS[@]} + 1)) files)"

# ----------------------------------------------------------------------------
# 3. Build the framework
# ----------------------------------------------------------------------------

build_proj() {
    local proj="$1"
    echo "==> Building $(basename "$proj")"
    dotnet build "$SCRIPT_DIR/$proj" -c Release --nologo -v minimal
}

build_proj "src/ModSdk"
build_proj "src/GameUI"
build_proj "src/ModHost"
build_proj "src/Patcher"

MODSDK_DLL="$SCRIPT_DIR/src/ModSdk/bin/Release/Gambonanza.ModSdk.dll"
GAMEUI_DLL="$SCRIPT_DIR/src/GameUI/bin/Release/Gambonanza.GameUI.dll"
MODHOST_DLL="$SCRIPT_DIR/src/ModHost/bin/Release/Gambonanza.ModHost.dll"
PATCHER_DLL="$SCRIPT_DIR/src/Patcher/bin/Release/net8.0/GambonanzaPatcher.dll"

for f in "$MODSDK_DLL" "$GAMEUI_DLL" "$MODHOST_DLL" "$PATCHER_DLL"; do
    [ -f "$f" ] || { echo "missing build output: $f" >&2; exit 1; }
done

# ----------------------------------------------------------------------------
# 4. Patch the game
# ----------------------------------------------------------------------------

echo "==> Patching Assembly-CSharp.dll (installs ModSdk + ModHost + GameUI)"
dotnet "$PATCHER_DLL" "$MANAGED_DIR" "$MODSDK_DLL" "$MODHOST_DLL" "$GAMEUI_DLL"

COMMIT="$(git -C "$SCRIPT_DIR" rev-parse HEAD 2>/dev/null || printf 'unknown')"
json_escape() {
    # Keep the installer dependency-free: no Python/jq required just to write metadata.
    printf '%s' "$1" | sed 's/\\/\\\\/g; s/"/\\"/g'
}
cat > "$MANAGED_DIR/Gambonanza.ModHost.install.json" <<EOF
{
  "version": "$(json_escape "$FRAMEWORK_VERSION")",
  "commit": "$(json_escape "$COMMIT")",
  "repoDir": "$(json_escape "$SCRIPT_DIR")",
  "gameDir": "$(json_escape "$GAME_DIR")",
  "appId": "3509230"
}
EOF
echo "  metadata -> $MANAGED_DIR/Gambonanza.ModHost.install.json (v$FRAMEWORK_VERSION, ${COMMIT:0:7})"

mkdir -p "$MODS_DIR"

# ----------------------------------------------------------------------------
# 5. Build & install sample mods
# ----------------------------------------------------------------------------

if [ "$SKIP_SAMPLES" -eq 1 ]; then
    echo
    echo "Done. Framework installed; sample mods skipped (--skip-samples)."
    echo "Drop your own mod folders into $MODS_DIR/ and launch from Steam."
    exit 0
fi

DIST_DIR="$SCRIPT_DIR/Mods"
mkdir -p "$DIST_DIR"

# Each entry: "<source folder>:<assembly name>:<extra asset 1> <extra asset 2> ..."
SAMPLES=(
    "SpeedMod:Gambonanza.SpeedMod:"
    "GambitApi:Gambonanza.GambitApi:"
    "KamikazeGambit:Gambonanza.KamikazeGambit:kamikaze.png"
    "EnemyThreatOverlay:Gambonanza.EnemyThreatOverlay:"
    "MightyKasparovEveryStage:Gambonanza.MightyKasparovEveryStage:"
)

for entry in "${SAMPLES[@]}"; do
    IFS=":" read -r mod asm assets <<<"$entry"
    src="$SCRIPT_DIR/sample_mods/$mod"
    out="$DIST_DIR/$mod"
    live="$MODS_DIR/$mod"

    echo "==> Building sample: $mod"
    dotnet build "$src" -c Release --nologo -v minimal

    dll="$src/bin/Release/$asm.dll"
    [ -f "$dll" ] || { echo "missing build output: $dll" >&2; exit 1; }

    rm -rf "$out" "$live"
    mkdir -p "$out" "$live"
    cp "$dll" "$out/"
    cp "$src/mod.json" "$out/"
    for a in $assets; do cp "$src/$a" "$out/"; done
    cp -R "$out/." "$live/"
    echo "  staged    -> $out"
    echo "  installed -> $live"
done

echo
echo "All done. Sample mods installed in $MODS_DIR/."
echo "Launch the game from Steam — press F10, F1, or backtick to open"
echo "the in-game console. Type 'help' to list commands."
