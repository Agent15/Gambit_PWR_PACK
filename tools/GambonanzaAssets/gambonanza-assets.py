#!/usr/bin/env python3
"""
GambonanzaAssets - visual asset editor for Gambonanza.

Swaps the game's images without touching a line of code. Run it, a browser
opens, you get a searchable gallery of every sprite and texture in the game.
Click one, download the PNG, draw over it, drop it back in, hit Apply.

Everything is reversible: the original .assets files are backed up before the
first patch and "Restore original" puts them back byte for byte.

    python3 gambonanza-assets.py                     # find the game, open the UI
    python3 gambonanza-assets.py --game /path/to/Gambonanza
    python3 gambonanza-assets.py --restore           # undo all patches, no UI
    python3 gambonanza-assets.py --reindex           # drop the cached index and rebuild

Requires: Python 3.8+, UnityPy, Pillow. The launcher scripts install them.
"""

from __future__ import annotations

import argparse
import io
import json
import os
import re
import shutil
import subprocess
import sys
import threading
import time
import webbrowser
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import unquote, urlparse

HERE = Path(__file__).resolve().parent
PACK_DIR = HERE / "pack"
CACHE_DIR = HERE / ".cache"
BACKUP_DIRNAME = "_GambonanzaAssets_vanilla_backup"

# Asset files worth scanning. level0 holds scene objects but no image data.
ASSET_FILES = ["globalgamemanagers.assets", "resources.assets", "sharedassets0.assets"]

STEAM_APP_ID = "3509230"


# ---------------------------------------------------------------------------
# Locating the game
# ---------------------------------------------------------------------------

def game_dir_from_framework_install() -> Path | None:
    """
    If the GambonanzaMods framework has been installed, it wrote the resolved game
    path into Managed/Gambonanza.ModHost.install.json. Nothing else knows about a
    non-default Steam library, so this is the best hint available.
    """
    for default in _DEFAULT_GAME_DIRS:
        data = find_data_dir(default)
        if not data:
            continue
        meta = data / "Managed" / "Gambonanza.ModHost.install.json"
        try:
            blob = json.loads(meta.read_text())
        except (OSError, ValueError):
            continue
        for key in ("gameDir", "gameDirNative"):
            if blob.get(key):
                return Path(blob[key])
    return None


_DEFAULT_GAME_DIRS = [
    Path.home() / "Library/Application Support/Steam/steamapps/common/Gambonanza",
    Path.home() / ".local/share/Steam/steamapps/common/Gambonanza",
    Path.home() / ".steam/steam/steamapps/common/Gambonanza",
    Path("C:/Program Files (x86)/Steam/steamapps/common/Gambonanza"),
    Path("C:/Program Files/Steam/steamapps/common/Gambonanza"),
]


def game_dir_candidates() -> list[Path]:
    found = game_dir_from_framework_install()
    return ([found] if found else []) + _DEFAULT_GAME_DIRS


def find_data_dir(game_dir: Path) -> Path | None:
    """Locate the *_Data directory inside an install, across all three platforms."""
    for sub in (
        "Gambonanza.app/Contents/Resources/Data",
        "Gambonanza_Data",
        "Gambonanza/Gambonanza_Data",
    ):
        d = game_dir / sub
        if (d / "globalgamemanagers").exists():
            return d
    return None


def resolve_game(explicit: str | None) -> tuple[Path, Path]:
    """Returns (game_dir, data_dir) or exits with a readable message."""
    tried = []
    candidates = []
    if explicit:
        candidates.append(Path(explicit).expanduser())
    if os.environ.get("GAMBONANZA_DIR"):
        candidates.append(Path(os.environ["GAMBONANZA_DIR"]).expanduser())
    candidates += game_dir_candidates()

    for c in candidates:
        tried.append(str(c))
        if not c.exists():
            continue
        data = find_data_dir(c)
        if data:
            return c, data
        # Maybe they pointed straight at the Data folder or the .app.
        for probe in (c, c / "Contents/Resources/Data"):
            if (probe / "globalgamemanagers").exists():
                return probe.parent, probe

    sys.exit(
        "Could not find your Gambonanza install.\n\n"
        "Pass the folder explicitly, e.g.\n"
        "    python3 gambonanza-assets.py --game \"/path/to/steamapps/common/Gambonanza\"\n\n"
        "Looked in:\n  " + "\n  ".join(tried)
    )


def steam_build_id(game_dir: Path) -> str:
    acf = game_dir.parent.parent / f"appmanifest_{STEAM_APP_ID}.acf"
    try:
        m = re.search(r'"buildid"\s*"(\d+)"', acf.read_text(errors="ignore"))
        return m.group(1) if m else "unknown"
    except OSError:
        return "unknown"


# ---------------------------------------------------------------------------
# Categories
#
# Ordered rules - first match wins. Keeps the gallery navigable without needing
# to know Unity naming conventions.
# ---------------------------------------------------------------------------

CATEGORY_RULES: list[tuple[str, str]] = [
    ("Chess pieces",       r"pieces?blanches|pieces?noires|spr_chesspiece|_[bw]$|pawn|rook|knight|bishop|queen|king"),
    ("Gambit icons",       r"spr_gambits|gambit"),
    ("Board & tiles",      r"tile|square|board|checker"),
    ("Bosses",             r"^boss_|spr_boss|onikaru|maskaruga|geisha|finalboss|portal"),
    ("Ascension & medals", r"ascend|medal|trophy"),
    ("Minigames",          r"gachapon|pachinko|wheels|slot"),
    ("Title & branding",   r"title|logo|publisher|splash|bluku"),
    ("Effects & particles", r"godray|fog|fade|circle|noise|glow|aura|shadow|spark|smoke|ray|dot|triangle|tentacle|arm|sweat|fist|lemnisate|distort|ripple|crt"),
    ("Cursors & arrows",   r"cursor|arrow|selection|highlight"),
    ("Icons & UI",         r"spr_ui|icon|button|check|cross|lock|pan_|paste|thumbup|panel|window|bar|slider"),
    ("Controller glyphs",  r"xbox|nintendo|switch|steamdeck|gampad|gamepad|glyph|controller|buttons pack"),
    ("Social",             r"discord|steam|twitch"),
    ("Fonts & text",       r"\bsdf\b|font|atlas|emojione|jersey|notosans|liberation|vcrosd"),
]

# Categories a non-technical user should not casually break. Still browsable,
# just behind a toggle - replacing an SDF font atlas garbles all text in the game.
TECHNICAL_CATEGORIES = {"Fonts & text", "Effects & particles", "Controller glyphs"}


def categorise(name: str) -> str:
    low = (name or "").lower()
    for label, pattern in CATEGORY_RULES:
        if re.search(pattern, low):
            return label
    return "Other"


PRETTY_SUBS = [
    (r"^spr[_ ]?", ""),
    (r"^tex[_ ]?", ""),
    (r"_", " "),
]


def pretty(name: str) -> str:
    out = name or ""
    for pat, rep in PRETTY_SUBS:
        out = re.sub(pat, rep, out, flags=re.IGNORECASE)
    return re.sub(r"\s+", " ", out).strip() or name


# ---------------------------------------------------------------------------
# Index
# ---------------------------------------------------------------------------

class Index:
    """
    Catalogue of every editable image in the game.

    Two kinds of entry:
      texture - a whole image file inside the game's asset archive
      sprite  - a named rectangle carved out of a texture (an atlas region)

    Sprites are what the game actually draws, and what people recognise ("the
    Warlock gambit icon"), so they are the default browse unit. Editing a sprite
    pastes the replacement back into its parent atlas at the right rectangle.
    """

    def __init__(self, data_dir: Path):
        self.data_dir = data_dir
        self.entries: dict[str, dict] = {}
        self._envs: dict[str, object] = {}
        self._obj_maps: dict[str, dict] = {}
        self._lock = threading.RLock()

    # -- UnityPy environments, loaded once and kept warm -------------------

    def env(self, file_key: str):
        import UnityPy
        with self._lock:
            if file_key not in self._envs:
                self._envs[file_key] = UnityPy.load(str(self.data_dir / file_key))
            return self._envs[file_key]

    def obj_map(self, file_key: str) -> dict:
        """
        path_id -> object, built once per asset file.

        sharedassets0 holds 170k objects; scanning that list per thumbnail made
        the gallery take minutes to fill in. One dict turns every later lookup
        into O(1).
        """
        with self._lock:
            if file_key not in self._obj_maps:
                self._obj_maps[file_key] = {o.path_id: o for o in self.env(file_key).objects}
            return self._obj_maps[file_key]

    def drop_envs(self):
        with self._lock:
            self._envs.clear()
            self._obj_maps.clear()

    # -- Build -------------------------------------------------------------

    def signature(self) -> str:
        parts = []
        for f in ASSET_FILES:
            p = self.data_dir / f
            parts.append(f"{f}:{p.stat().st_size if p.exists() else 0}")
        return "|".join(parts)

    def cache_path(self) -> Path:
        return CACHE_DIR / "index.json"

    def load_cached(self) -> bool:
        try:
            blob = json.loads(self.cache_path().read_text())
        except (OSError, ValueError):
            return False
        if blob.get("signature") != self.signature():
            return False
        self.entries = blob["entries"]
        return True

    def save_cache(self):
        CACHE_DIR.mkdir(parents=True, exist_ok=True)
        self.cache_path().write_text(json.dumps({
            "signature": self.signature(),
            "entries": self.entries,
        }))

    def build(self, progress=lambda msg: None):
        self.entries = {}
        for file_key in ASSET_FILES:
            path = self.data_dir / file_key
            if not path.exists():
                continue
            progress(f"Reading {file_key}…")
            env = self.env(file_key)

            textures: dict[int, dict] = {}
            for obj in env.objects:
                if obj.type.name != "Texture2D":
                    continue
                try:
                    d = obj.read()
                except Exception:
                    continue
                w, h = int(d.m_Width), int(d.m_Height)
                name = d.m_Name or f"Texture_{obj.path_id}"
                textures[obj.path_id] = {
                    "id": f"{file_key}|t{obj.path_id}",
                    "kind": "texture",
                    "file": file_key,
                    "pathId": obj.path_id,
                    "name": name,
                    "label": pretty(name),
                    "width": w,
                    "height": h,
                    "format": format_name(getattr(d, "m_TextureFormat", "?")),
                    "category": categorise(name),
                    # 0x0 textures are placeholders Unity fills at runtime - nothing to edit.
                    "editable": w > 0 and h > 0,
                    "sprites": [],
                }

            progress(f"Mapping sprites in {file_key}…")
            for obj in env.objects:
                if obj.type.name != "Sprite":
                    continue
                try:
                    d = obj.read()
                    rd = d.m_RD
                    tex_id = rd.texture.path_id
                    rect = rd.textureRect
                except Exception:
                    continue
                parent = textures.get(tex_id)
                if parent is None:
                    # Atlas lives in another asset file (controller glyph packs do
                    # this). We can't edit it in place, so skip rather than show a
                    # broken entry.
                    continue
                name = d.m_Name or f"Sprite_{obj.path_id}"
                w, h = int(rect.width), int(rect.height)
                if w <= 0 or h <= 0:
                    continue
                entry = {
                    "id": f"{file_key}|s{obj.path_id}",
                    "kind": "sprite",
                    "file": file_key,
                    "pathId": obj.path_id,
                    "name": name,
                    "label": pretty(name),
                    "width": w,
                    "height": h,
                    "format": parent["format"],
                    # A sprite inherits its parent atlas's category unless its own
                    # name is more specific (atlas "SPR_Icons" vs sprite "SPR_Icons_Boss").
                    "category": categorise(name) if categorise(name) != "Other" else parent["category"],
                    "editable": True,
                    "atlas": parent["name"],
                    "atlasId": parent["id"],
                    "rect": [int(rect.x), int(rect.y), w, h],
                }
                self.entries[entry["id"]] = entry
                parent["sprites"].append(entry["id"])

            for t in textures.values():
                self.entries[t["id"]] = t

        progress("Done.")
        self.save_cache()

    # -- Image access ------------------------------------------------------

    def _unity_obj(self, entry: dict):
        return self.obj_map(entry["file"]).get(entry["pathId"])

    def image(self, entry_id: str):
        """Full-resolution PIL image for an entry."""
        entry = self.entries[entry_id]
        obj = self._unity_obj(entry)
        if obj is None:
            raise KeyError(entry_id)
        return obj.read().image.convert("RGBA")

    def thumbnail(self, entry_id: str, box: int = 192) -> bytes:
        cache = CACHE_DIR / "thumbs" / f"{safe_filename(entry_id)}.png"
        if cache.exists():
            return cache.read_bytes()

        from PIL import Image
        img = self.image(entry_id)
        # Only ever downscale here, and smoothly - the browser upscales small
        # pixel-art sprites with image-rendering:pixelated, which stays crisp and
        # keeps the cached files tiny.
        if max(img.size) > box:
            img = img.copy()
            img.thumbnail((box, box), Image.LANCZOS)
        buf = io.BytesIO()
        img.save(buf, format="PNG")
        data = buf.getvalue()
        cache.parent.mkdir(parents=True, exist_ok=True)
        cache.write_bytes(data)
        return data

    def warm_thumbnails(self, progress=lambda msg: None):
        """
        Pre-render every thumbnail in the background so scrolling the gallery is
        instant. Decoding a DXT atlas is the slow part and it only has to happen
        once per game build - after that the disk cache serves everything.
        """
        todo = [e for e in self.entries.values() if e.get("editable")]
        done = 0
        for entry in todo:
            try:
                self.thumbnail(entry["id"])
            except Exception:
                pass  # a broken asset shouldn't stall the rest
            done += 1
            if done % 50 == 0:
                progress(f"Rendering previews… {done}/{len(todo)}")
        progress("Previews ready.")


def safe_filename(s: str) -> str:
    return re.sub(r"[^A-Za-z0-9._-]", "_", s)


# UnityPy hands back the raw enum value on read. "12" means nothing to anyone;
# "DXT5 (compressed)" at least hints why a re-saved atlas can look slightly softer.
_TEXTURE_FORMATS = {
    1: "Alpha8", 2: "ARGB4444", 3: "RGB24", 4: "RGBA32", 5: "ARGB32",
    7: "RGB565", 9: "R16", 10: "DXT1 (compressed)", 12: "DXT5 (compressed)",
    13: "RGBA4444", 14: "BGRA32", 17: "RHalf", 20: "RFloat", 22: "RGB9e5Float",
    24: "BC6H", 25: "BC7", 26: "BC4", 27: "BC5", 34: "RG16", 47: "RG32",
    48: "RGB48", 49: "RGBA64",
}


def format_name(raw) -> str:
    text = str(raw).replace("TextureFormat.", "")
    try:
        return _TEXTURE_FORMATS.get(int(text), f"format {text}")
    except ValueError:
        return text


# ---------------------------------------------------------------------------
# The pack - staged edits, applied together
# ---------------------------------------------------------------------------

class Pack:
    def __init__(self, index: Index, data_dir: Path):
        self.index = index
        self.data_dir = data_dir
        PACK_DIR.mkdir(parents=True, exist_ok=True)

    @property
    def manifest_path(self) -> Path:
        return PACK_DIR / "pack.json"

    def load(self) -> dict:
        try:
            return json.loads(self.manifest_path.read_text())
        except (OSError, ValueError):
            return {}

    def save(self, manifest: dict):
        self.manifest_path.write_text(json.dumps(manifest, indent=2))

    def png_path(self, entry_id: str) -> Path:
        return PACK_DIR / f"{safe_filename(entry_id)}.png"

    def stage(self, entry_id: str, png_bytes: bytes, autofit: bool = True) -> dict:
        from PIL import Image
        entry = self.index.entries[entry_id]
        if not entry.get("editable"):
            raise ValueError(
                f"“{entry['name']}” has no pixel data of its own - Unity fills it in at "
                "runtime, so there is nothing to replace."
            )
        img = Image.open(io.BytesIO(png_bytes)).convert("RGBA")
        target = (entry["width"], entry["height"])
        resized = False
        if img.size != target:
            if not autofit:
                raise ValueError(
                    f"Image is {img.size[0]}x{img.size[1]} but this asset is "
                    f"{target[0]}x{target[1]}. Turn on auto-fit or resize it yourself."
                )
            # NEAREST keeps pixel art readable; anything else turns 21px icons to mush.
            img = img.resize(target, Image.NEAREST)
            resized = True

        self.png_path(entry_id).write_bytes(to_png(img))
        manifest = self.load()
        manifest[entry_id] = {
            "name": entry["name"],
            "kind": entry["kind"],
            "file": entry["file"],
            "stagedAt": time.strftime("%Y-%m-%d %H:%M:%S"),
            "resized": resized,
        }
        self.save(manifest)
        return {"resized": resized, "width": target[0], "height": target[1]}

    def unstage(self, entry_id: str):
        self.png_path(entry_id).unlink(missing_ok=True)
        manifest = self.load()
        manifest.pop(entry_id, None)
        self.save(manifest)

    def clear(self):
        for entry_id in list(self.load()):
            self.png_path(entry_id).unlink(missing_ok=True)
        self.save({})

    # -- Backup / restore --------------------------------------------------

    @property
    def backup_dir(self) -> Path:
        return self.data_dir / BACKUP_DIRNAME

    @property
    def ledger_path(self) -> Path:
        return self.backup_dir / "backup.json"

    def ledger(self) -> dict:
        try:
            return json.loads(self.ledger_path.read_text())
        except (OSError, ValueError):
            return {}

    def write_ledger(self, data: dict):
        self.backup_dir.mkdir(parents=True, exist_ok=True)
        self.ledger_path.write_text(json.dumps(data, indent=2))

    def is_patched(self) -> bool:
        led = self.ledger()
        return any(
            v.get("patchedSha") and sha256_of(self.data_dir / f) == v["patchedSha"]
            for f, v in led.items()
        )

    def backup_once(self, files: list[str], log=print):
        """
        Keep a pristine copy of every file we are about to rewrite.

        Steam updates are the trap here: after one, the installed .assets file is
        a *new* vanilla that no longer matches the backup we took months ago.
        Restoring the old backup at that point would roll the game's art back to
        the previous version and desync it from the rest of the install. So we
        record hashes and refresh the backup whenever the live file is neither the
        vanilla we saved nor the patched output we last wrote.
        """
        self.backup_dir.mkdir(parents=True, exist_ok=True)
        led = self.ledger()
        for f in files:
            live = self.data_dir / f
            dst = self.backup_dir / f
            live_sha = sha256_of(live)
            rec = led.get(f, {})

            if not dst.exists():
                log(f"Backing up {f}…")
                shutil.copy2(live, dst)
                led[f] = {"vanillaSha": live_sha, "patchedSha": None}
                continue

            if live_sha in (rec.get("vanillaSha"), rec.get("patchedSha")):
                continue  # backup still describes this install

            if not rec:
                # A backup with no ledger entry - an older GambonanzaAssets took it, or the
                # ledger was deleted. Trust it if it still matches something sane.
                backup_sha = sha256_of(dst)
                led[f] = {
                    "vanillaSha": backup_sha,
                    "patchedSha": live_sha if live_sha != backup_sha else None,
                }
                continue

            log(f"{f} changed outside GambonanzaAssets (game update?) - refreshing the backup.")
            shutil.copy2(live, dst)
            led[f] = {"vanillaSha": live_sha, "patchedSha": None}
        self.write_ledger(led)

    def note_patched(self, file_key: str):
        led = self.ledger()
        rec = led.setdefault(file_key, {})
        rec["patchedSha"] = sha256_of(self.data_dir / file_key)
        self.write_ledger(led)

    def restore(self, log=print) -> int:
        if not self.backup_dir.exists():
            return 0
        led = self.ledger()
        n = 0
        for src in sorted(self.backup_dir.glob("*.assets")):
            log(f"Restoring {src.name}…")
            shutil.copy2(src, self.data_dir / src.name)
            led.setdefault(src.name, {})["patchedSha"] = None
            n += 1
        self.write_ledger(led)
        self.index.drop_envs()
        return n

    # -- Apply -------------------------------------------------------------

    def apply(self, log=print) -> dict:
        """
        Write every staged edit into the game's asset files.

        Always patches from the pristine backup so applying twice never stacks,
        and removing an edit from the pack genuinely removes it from the game.
        """
        from PIL import Image

        manifest = self.load()
        if not manifest:
            return {"changed": 0, "files": []}

        by_file: dict[str, list[str]] = {}
        for entry_id in manifest:
            entry = self.index.entries.get(entry_id)
            if entry is None:
                # The asset this edit targeted is gone - almost always a game update
                # that renumbered things. Say so rather than silently dropping it.
                log(f"Skipping “{manifest[entry_id].get('name', entry_id)}”: no longer "
                    f"in the game's assets (run with --reindex after a game update).")
                continue
            by_file.setdefault(entry["file"], []).append(entry_id)

        self.backup_once(list(by_file), log=log)
        self.index.drop_envs()

        import UnityPy

        changed = 0
        for file_key, entry_ids in by_file.items():
            log(f"Patching {file_key} ({len(entry_ids)} change(s))…")

            # Put the vanilla file back first, then patch it in place. Two reasons:
            #  - applying twice never stacks, and dropping an edit from the pack
            #    genuinely removes it from the game;
            #  - most textures stream their pixels from a sibling .resS file, and
            #    UnityPy resolves that path relative to the file it loaded. Loading
            #    straight out of the backup folder can't find sharedassets0.assets.resS.
            shutil.copy2(self.backup_dir / file_key, self.data_dir / file_key)
            env = UnityPy.load(str(self.data_dir / file_key))

            objects = {o.path_id: o for o in env.objects}

            # Sprite edits are grouped by their parent atlas: several sprites can
            # share one texture, and each must be pasted into the same decoded
            # image before it gets re-encoded once.
            atlas_edits: dict[int, list[tuple[dict, Image.Image]]] = {}
            texture_edits: dict[int, Image.Image] = {}

            for entry_id in entry_ids:
                entry = self.index.entries[entry_id]
                png = self.png_path(entry_id)
                if not png.exists():
                    continue
                img = Image.open(png).convert("RGBA")
                if entry["kind"] == "texture":
                    texture_edits[entry["pathId"]] = img
                else:
                    sprite_obj = objects.get(entry["pathId"])
                    if sprite_obj is None:
                        continue
                    tex_path_id = sprite_obj.read().m_RD.texture.path_id
                    atlas_edits.setdefault(tex_path_id, []).append((entry, img))

            for tex_path_id, edits in atlas_edits.items():
                obj = objects.get(tex_path_id)
                if obj is None:
                    continue
                tex = obj.read()
                atlas = tex.image.convert("RGBA")
                for entry, img in edits:
                    x, y, w, h = entry["rect"]
                    # Unity texture rects are bottom-left origin; PIL is top-left.
                    top = atlas.height - y - h
                    atlas.paste(img, (x, top))
                    changed += 1
                tex.image = atlas
                tex.save()

            for path_id, img in texture_edits.items():
                obj = objects.get(path_id)
                if obj is None:
                    continue
                tex = obj.read()
                tex.image = img
                tex.save()
                changed += 1

            tmp_dir = CACHE_DIR / "out"
            if tmp_dir.exists():
                shutil.rmtree(tmp_dir)
            tmp_dir.mkdir(parents=True, exist_ok=True)
            env.save(out_path=str(tmp_dir))

            produced = tmp_dir / file_key
            if not produced.exists():
                raise RuntimeError(f"UnityPy did not write {file_key}")
            shutil.move(str(produced), str(self.data_dir / file_key))
            self.note_patched(file_key)

        self.index.drop_envs()
        return {"changed": changed, "files": sorted(by_file)}


def to_png(img) -> bytes:
    buf = io.BytesIO()
    img.save(buf, format="PNG")
    return buf.getvalue()


def sha256_of(path: Path) -> str:
    import hashlib
    h = hashlib.sha256()
    try:
        with open(path, "rb") as fh:
            for chunk in iter(lambda: fh.read(1 << 20), b""):
                h.update(chunk)
    except OSError:
        return ""
    return h.hexdigest()


def game_is_running() -> bool:
    """
    Unity keeps its .assets files mapped while the game is up, so patching them
    underneath a running game either fails or corrupts the session.

    Matches on the *process name*, not on any path containing "Gambonanza" - the
    tool itself usually lives in a folder called GambonanzaMods, and a substring
    check would flag GambonanzaAssets as the game and refuse to ever do anything.
    """
    try:
        if sys.platform == "win32":
            out = subprocess.run(
                ["tasklist", "/FI", "IMAGENAME eq Gambonanza.exe", "/NH"],
                capture_output=True, text=True, timeout=10,
            ).stdout
            return "Gambonanza.exe" in out
        return subprocess.run(
            ["pgrep", "-x", "Gambonanza"], capture_output=True, timeout=10
        ).returncode == 0
    except Exception:
        return False


# ---------------------------------------------------------------------------
# HTTP server
# ---------------------------------------------------------------------------

class App:
    def __init__(self, game_dir: Path, data_dir: Path):
        self.game_dir = game_dir
        self.data_dir = data_dir
        self.index = Index(data_dir)
        self.pack = Pack(self.index, data_dir)
        self.build_log: list[str] = []
        self.ready = False

    def ensure_index(self, force: bool = False):
        if not force and self.index.load_cached():
            self.ready = True
            return
        self.index.build(progress=self.build_log.append)
        self.ready = True

    def status(self) -> dict:
        manifest = self.pack.load()
        return {
            "gameDir": str(self.game_dir),
            "dataDir": str(self.data_dir),
            "steamBuildId": steam_build_id(self.game_dir),
            "patched": self.pack.is_patched(),
            "pending": len(manifest),
            "pack": manifest,
            "gameRunning": game_is_running(),
            "ready": self.ready,
            "log": self.build_log[-12:],
        }


def make_handler(app: App):
    ui_html = (HERE / "ui.html").read_bytes()

    class Handler(BaseHTTPRequestHandler):
        protocol_version = "HTTP/1.1"

        def log_message(self, *a):
            pass  # keep the terminal clean for the humans

        # -- helpers ------------------------------------------------------

        def send(self, code: int, body: bytes, ctype: str, cache: bool = False):
            self.send_response(code)
            self.send_header("Content-Type", ctype)
            self.send_header("Content-Length", str(len(body)))
            if cache:
                self.send_header("Cache-Control", "max-age=86400")
            self.end_headers()
            self.wfile.write(body)

        def send_json(self, obj, code: int = 200):
            self.send(code, json.dumps(obj).encode(), "application/json")

        def read_body(self) -> bytes:
            length = int(self.headers.get("Content-Length") or 0)
            return self.rfile.read(length) if length else b""

        # -- routes -------------------------------------------------------

        def do_GET(self):
            path = urlparse(self.path).path
            try:
                if path in ("/", "/index.html"):
                    return self.send(200, ui_html, "text/html; charset=utf-8")

                if path == "/api/status":
                    return self.send_json(app.status())

                if path == "/api/index":
                    if not app.ready:
                        return self.send_json({"ready": False, "log": app.build_log[-12:]})
                    entries = [
                        {k: v for k, v in e.items() if k != "sprites"}
                        for e in app.index.entries.values()
                    ]
                    entries.sort(key=lambda e: (e["category"], e["label"].lower()))
                    counts: dict[str, int] = {}
                    for e in entries:
                        counts[e["category"]] = counts.get(e["category"], 0) + 1
                    return self.send_json({
                        "ready": True,
                        "entries": entries,
                        "categories": [
                            {"name": k, "count": v, "technical": k in TECHNICAL_CATEGORIES}
                            for k, v in sorted(counts.items())
                        ],
                    })

                if path.startswith("/api/thumb/"):
                    entry_id = unquote(path[len("/api/thumb/"):])
                    return self.send(200, app.index.thumbnail(entry_id), "image/png", cache=True)

                if path.startswith("/api/full/"):
                    entry_id = unquote(path[len("/api/full/"):])
                    entry = app.index.entries[entry_id]
                    staged = app.pack.png_path(entry_id)
                    data = staged.read_bytes() if staged.exists() else to_png(app.index.image(entry_id))
                    self.send_response(200)
                    self.send_header("Content-Type", "image/png")
                    self.send_header("Content-Length", str(len(data)))
                    self.send_header(
                        "Content-Disposition",
                        f'attachment; filename="{safe_filename(entry["name"])}.png"',
                    )
                    self.end_headers()
                    return self.wfile.write(data)

                if path.startswith("/api/staged/"):
                    entry_id = unquote(path[len("/api/staged/"):])
                    staged = app.pack.png_path(entry_id)
                    if not staged.exists():
                        return self.send_json({"error": "not staged"}, 404)
                    return self.send(200, staged.read_bytes(), "image/png")

                return self.send_json({"error": "not found"}, 404)

            except KeyError:
                self.send_json({"error": "unknown asset"}, 404)
            except Exception as exc:  # surface real errors in the UI, don't 500 silently
                self.send_json({"error": f"{type(exc).__name__}: {exc}"}, 500)

        def do_POST(self):
            path = urlparse(self.path).path
            try:
                if path.startswith("/api/replace/"):
                    entry_id = unquote(path[len("/api/replace/"):])
                    autofit = self.headers.get("X-Autofit", "1") != "0"
                    result = app.pack.stage(entry_id, self.read_body(), autofit=autofit)
                    return self.send_json({"ok": True, **result})

                if path.startswith("/api/unstage/"):
                    entry_id = unquote(path[len("/api/unstage/"):])
                    app.pack.unstage(entry_id)
                    return self.send_json({"ok": True})

                if path == "/api/apply":
                    if game_is_running():
                        return self.send_json(
                            {"error": "Close Gambonanza first - the game holds its asset files open."}, 409
                        )
                    log: list[str] = []
                    result = app.pack.apply(log=log.append)
                    return self.send_json({"ok": True, "log": log, **result})

                if path == "/api/restore":
                    if game_is_running():
                        return self.send_json(
                            {"error": "Close Gambonanza first - the game holds its asset files open."}, 409
                        )
                    log: list[str] = []
                    n = app.pack.restore(log=log.append)
                    return self.send_json({"ok": True, "restored": n, "log": log})

                if path == "/api/pack/clear":
                    app.pack.clear()
                    return self.send_json({"ok": True})

                return self.send_json({"error": "not found"}, 404)

            except ValueError as exc:
                self.send_json({"error": str(exc)}, 400)
            except KeyError:
                self.send_json({"error": "unknown asset"}, 404)
            except Exception as exc:
                self.send_json({"error": f"{type(exc).__name__}: {exc}"}, 500)

    return Handler


# ---------------------------------------------------------------------------

def main():
    ap = argparse.ArgumentParser(description="Visual asset editor for Gambonanza.")
    ap.add_argument("--game", help="Path to the Gambonanza install folder")
    ap.add_argument("--port", type=int, default=8770)
    ap.add_argument("--no-browser", action="store_true")
    ap.add_argument("--reindex", action="store_true", help="Rebuild the asset index from scratch")
    ap.add_argument("--restore", action="store_true", help="Restore vanilla assets and exit")
    args = ap.parse_args()

    try:
        import UnityPy  # noqa: F401
        from PIL import Image  # noqa: F401
    except ImportError:
        sys.exit(
            "GambonanzaAssets needs two Python packages. Install them with:\n\n"
            "    python3 -m pip install UnityPy Pillow\n"
        )

    game_dir, data_dir = resolve_game(args.game)
    app = App(game_dir, data_dir)

    print(f"Gambonanza : {game_dir}")
    print(f"Assets     : {data_dir}")
    print(f"Steam build: {steam_build_id(game_dir)}")

    if args.restore:
        n = app.pack.restore()
        print(f"Restored {n} vanilla asset file(s)." if n else "Nothing to restore - game is already vanilla.")
        return

    if args.reindex:
        shutil.rmtree(CACHE_DIR, ignore_errors=True)

    print("Indexing game assets… (first run takes a few seconds)")
    app.ensure_index(force=args.reindex)
    n_sprites = sum(1 for e in app.index.entries.values() if e["kind"] == "sprite")
    n_textures = sum(1 for e in app.index.entries.values() if e["kind"] == "texture")
    print(f"Found {n_sprites} sprites and {n_textures} textures.")

    # Fill the thumbnail cache while the user is still reading the first screen.
    threading.Thread(
        target=app.index.warm_thumbnails,
        args=(app.build_log.append,),
        daemon=True,
    ).start()

    server = ThreadingHTTPServer(("127.0.0.1", args.port), make_handler(app))
    url = f"http://127.0.0.1:{args.port}/"
    print(f"\n  GambonanzaAssets is running at {url}")
    print("  Leave this window open. Press Ctrl+C when you're done.\n")

    if not args.no_browser:
        threading.Timer(0.6, lambda: webbrowser.open(url)).start()

    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nBye.")


if __name__ == "__main__":
    main()
