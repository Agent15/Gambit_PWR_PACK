# Modding Gambonanza's visuals (no code)

Everything in this document is about **pictures**. No C#, no `Assembly-CSharp.dll`
patching, no mod loader. If you want to change what the game *does*, you want
[sample_mods/README.md](../sample_mods/README.md) instead.

The short version: use [GambonanzaAssets](../tools/GambonanzaAssets/) and skip to the end. The rest
of this explains what it's doing, so you can do it by hand or with other tools if
you'd rather.

---

## Where the art lives

Gambonanza is a Unity 6000.4.3f1 game built the plain way: no AssetBundles, no
Addressables, no encryption. Every image is in one of three serialised archives:

```
Gambonanza.app/Contents/Resources/Data/          (macOS)
Gambonanza/Gambonanza_Data/                      (Windows / Linux)
├── globalgamemanagers.assets    3 textures     splash + publisher logos
├── resources.assets             5 textures     fonts, emoji, Switch glyphs
├── sharedassets0.assets       224 textures     ~everything you actually want
├── sharedassets0.assets.resS                   raw pixel bytes for the above
├── sharedassets0.resource                      audio
└── level0                                      scene objects, no images
```

The `.assets` files hold the metadata - name, width, height, pixel format - and
usually point at a sibling `.resS` file for the actual bytes. That split is why you
can't just open these in an image editor.

**236 textures**, and carved out of them, **925 named sprites** (as of game build 24613134 / v1.4.0).

---

## Textures vs sprites

Almost all of the art is atlased. One texture holds many pictures:

| Texture | Size | Holds |
| --- | --- | --- |
| `SPR_Gambits` | 512×512 | all 200 gambit icons (`SPR_Gambits_Warlock`, …) |
| `SPR_PiecesBlanches` / `SPR_PiecesNoires` | 2048×2048 | the white / black piece art |
| `SPR_ChessPieces` | 256×64 | `SPR_Queen_W`, `SPR_Knight_B`, … |
| `SPR_Icons` | 256×64 | `SPR_Icons_Skull`, `SPR_Icons_Boss`, … |
| `SPR_Boss_Clock`, `SPR_Boss_Fish`, … | 256×128 | one boss's body parts, ~15 sprites each |
| `Boss_1` … `Boss_8` | 1920×1080 | full-screen boss artwork |

A **Sprite** object records which texture it belongs to and a rectangle inside it.
So there are two ways to change a picture:

- **Replace a sprite** - paste a new image into that rectangle, leave the rest of
  the sheet alone. This is what you want 95% of the time.
- **Replace a texture** - swap the whole sheet. Every sprite on it changes at once.

Coordinate gotcha: Unity sprite rectangles are measured from the **bottom-left** of
the texture; every image library measures from the top-left. Flip with
`top = texture_height - rect.y - rect.height` or your art lands in the wrong place.

---

## Pixel formats

`m_TextureFormat` tells you what you're dealing with:

| Value | Meaning | Round-trips cleanly? |
| --- | --- | --- |
| 4 | RGBA32, uncompressed | Yes - exact |
| 3 | RGB24, uncompressed | Yes - exact |
| 10 / 12 | DXT1 / DXT5, block-compressed | Re-encoded on save; slight colour drift |
| 1 | Alpha8 | Yes |

Most of this game's pixel art is RGBA32, so edits are lossless. The big background
and boss plates are DXT5 - re-saving one recompresses the whole sheet, which can
nudge colours very slightly even in regions you didn't touch. It's not visible in
practice, but it's why you shouldn't repeatedly apply-restore-apply on a DXT atlas
and expect bit-identical results.

Also: when you write new pixel data in, it stops being streamed from `.resS` and
gets stored inline in the `.assets` file. That file grows. That's normal, and the
now-unused bytes in `.resS` are simply ignored.

---

## Doing it by hand

If you'd rather not use GambonanzaAssets, the moving parts are:

**Python + [UnityPy](https://github.com/K0lb3/UnityPy)** - the whole job in ~15 lines:

```python
import os
import UnityPy
from PIL import Image

env = UnityPy.load("sharedassets0.assets")           # run this from the Data folder
for obj in env.objects:
    if obj.type.name != "Sprite":
        continue
    sprite = obj.read()
    if sprite.m_Name != "SPR_Queen_W":
        continue

    tex = sprite.m_RD.texture.read()                 # the parent atlas
    rect = sprite.m_RD.textureRect
    atlas = tex.image.convert("RGBA")

    new = Image.open("my_queen.png").convert("RGBA")
    top = atlas.height - int(rect.y) - int(rect.height)   # bottom-left -> top-left
    atlas.paste(new, (int(rect.x), top))

    tex.image = atlas
    tex.save()
    break

os.makedirs("patched", exist_ok=True)                # env.save won't create it for you
env.save(out_path="patched")                         # then move patched/… into place
```

Two things that will bite you:

- **Back up `sharedassets0.assets` first.** There is no undo.
- **Load from the game's own `Data` folder.** UnityPy resolves `.resS` relative to
  the file you loaded, so a copy sitting somewhere else fails with
  `Resource file sharedassets0.assets.resS not found`.

**GUI alternatives** - [UABEA](https://github.com/nesrak1/UABEA) (browse and replace
textures by hand) and [AssetRipper](https://github.com/AssetRipper/AssetRipper)
(bulk-extract everything for reference). Both are Windows-first.

---

## The two hazards

**Steam updates overwrite everything.** An update ships fresh `.assets` files and
your art is gone. Re-apply and you're back. The subtler danger is restoring a
*stale* backup afterwards - that puts the previous version's art into the new build.
GambonanzaAssets hashes the files and re-takes its backup when it sees the game changed
underneath it; if you're rolling your own, do the same.

**Fonts are not art.** `*SDF Atlas` textures are signed-distance-field glyph sheets.
Painting on one corrupts every character the game renders. Leave them alone.

---

## When you want code instead

Asset patching edits the files on disk. The alternative is a **runtime override** -
a normal mod (see [sample_mods/README.md](../sample_mods/README.md)) that loads PNGs
from its own folder and assigns them to sprites as the game runs:

```csharp
var tex = new Texture2D(2, 2);
tex.LoadImage(File.ReadAllBytes(Path.Combine(ctx.ModDirectory, "queen.png")));
tex.filterMode = FilterMode.Point;      // pixel art: never let Unity blur it
```

Trade-offs:

|  | GambonanzaAssets (patch files) | Runtime override (mod DLL) |
| --- | --- | --- |
| Writing code | none | C# + a build |
| Survives a game update | no, re-apply | yes |
| Needs the framework installed | no | yes |
| Can change art conditionally | no | yes |
| Risk to the install | backed up, reversible | none, files untouched |

For a straight re-skin, patch the files. For anything that needs to react to game
state, write a mod.

---

## Quick start

```bash
cd tools/GambonanzaAssets
./gambonanza-assets.sh
```

Search, download, paint, drag back, Apply. Full walkthrough in
[tools/GambonanzaAssets/README.md](../tools/GambonanzaAssets/README.md).
