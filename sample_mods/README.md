# Gambonanza Sample Mods

Three reference mods that show the full range of what the Gambonanza modding
framework lets you do, from a one-file tweak to a full library that other mods
can build on.

```
sample_mods/
├── SpeedMod/         A 60-line mod that adds a settings row.
├── GambitApi/        A library mod — provides a builder for adding new gambits.
├── KamikazeGambit/   A custom gambit, built on top of GambitApi.
├── build.sh          Builds every mod and stages it into the repo's Mods/ folder.
└── README.md         You are here.
```

If you just want to play with the mods, run `./build.sh --install` and they
will be dropped into the live game's `Gambonanza/Mods/` directory.

If you want to write your own mod, read on.

---

## What is a Gambonanza mod?

A mod is a **.NET DLL** plus a **`mod.json`** file. The DLL contains a class
implementing `Gambonanza.ModSdk.IMod`; `mod.json` tells the loader which class
that is. Both files live together in `Gambonanza/Mods/<YourMod>/`.

When the game starts, `Gambonanza.ModHost` (installed in `Managed/` by the
patcher) walks every subfolder of `Mods/`, parses `mod.json`, calls
`Assembly.LoadFrom()` on the DLL, instantiates the entry class, and calls
`OnLoad`. From that moment your mod is a normal .NET object running inside
Unity — you can spawn `MonoBehaviour`s, hook into game classes via reflection,
swap `SpriteRenderer` materials, anything Unity allows.

There is no Harmony. The framework deliberately stays small: the patcher only
adds three call sites to `Assembly-CSharp.dll` and the rest is plain C#
reflection. If you need to patch a method, do it the hard way (replace the
field, watch a value in `LateUpdate`, instantiate a `MonoBehaviour` that wraps
the target). The samples here show several variations of this pattern.

---

## The mod manifest (`mod.json`)

```json
{
    "id":          "MyMod",
    "name":        "My Mod",
    "version":     "1.0.0",
    "author":      "your name",
    "entry":       "MyNamespace.MyModEntry",
    "enabled":     true,
    "gameVersion": ">=1.0",
    "description": "What your mod does, one line."
}
```

| Field         | Meaning                                                                                    |
| ------------- | ------------------------------------------------------------------------------------------ |
| `id`          | Unique identifier. Used by the in-game console and as the dictionary key in ModRegistry.     |
| `entry`       | Fully qualified class name that implements `IMod`. The loader scans every DLL in your mod folder for this type. |
| `enabled`     | If `false`, the mod is skipped at startup. Toggleable from the in-game console.             |
| `gameVersion` | Currently informational. Use `>=1.0`.                                                       |

---

## The IMod entry point

```csharp
using Gambonanza.ModSdk;

public sealed class MyModEntry : IMod
{
    public void OnLoad(IModContext ctx)
    {
        ctx.LogLine("MyMod is alive.");
    }
}
```

`IModContext` exposes:

- `ModId` / `ModDirectory` — useful for finding bundled assets next to the DLL.
- `LogLine(string)` — writes to `[ModHost] [<ModId>] <message>` in the Unity log.
- `Console` — shared in-game console for commands and messages.
- `OnSettingsOpened` — event fired with the `SettingsCanvas` MonoBehaviour every
  time the player opens the in-game settings panel. Subscribe here to inject
  custom rows (see `SpeedMod`).

That is the entire public API. Everything else is your code reaching into the
game via reflection.

---

## The three samples

### SpeedMod — the smallest possible mod

[`SpeedMod/SpeedModPlugin.cs`](SpeedMod/SpeedModPlugin.cs)

Adds a "Game Speed" arrow row to the settings canvas. Demonstrates:

- The `IMod` boilerplate.
- Subscribing to `IModContext.OnSettingsOpened`.
- Using `Gambonanza.GameUI.Pixel` (a helper library shipped alongside ModHost)
  to clone real game UI components instead of recreating them by hand.
- Setting `Time.timeScale` to mutate game speed.

Read this first — it is roughly 60 lines and touches every part of the
framework once.

### GambitApi — a library other mods build on

[`GambitApi/`](GambitApi/) — multi-file project.

Reverse-engineers the game's gambit registry to expose a fluent
`GambitBuilder` other mods can use to add new gambits:

```csharp
GambitBuilder.Create("MyMod_Coolio")
    .WithName("Coolio's Gambit")
    .WithDescription("Does cool things.")
    .WithRarity(Rarity.EPIC)
    .WithBaseGambit<MyGambitBehaviour>()
    .Register();
```

It also adds runtime patches to the in-game gambit collection screen so it
can paginate past 50 gambits, and a per-mod hook for picking up "extra"
gambits at runtime. Demonstrates the harder patterns:

- Reflecting on private `SerializeField` members of vanilla MonoBehaviours.
- Cloning a vanilla prefab to inherit its visuals, then swapping its scripts.
- Extending vanilla UI without touching `Assembly-CSharp.dll` (we do it from
  a `MonoBehaviour` attached at runtime; see `CollectionPaginationPatch.cs`).

GambitApi is itself a mod — it has its own `mod.json` and is loaded by
ModHost — but it is also a library: KamikazeGambit references it directly.

### KamikazeGambit — a real custom gambit

[`KamikazeGambit/`](KamikazeGambit/)

Adds a one-shot gambit: landing a piece on an enemy destroys both pieces.
Builds on `GambitApi` by passing its `GambitKamikaze` MonoBehaviour to
`.WithBaseGambit<T>()`. The interesting bits:

- `GambitKamikaze.cs` — the gambit's runtime behaviour. Subscribes to vanilla
  events, reads private state via reflection, and undoes the side-effects
  (e.g. restoring `tile.CanBeLandedOn` and `tile.PromoteColor`) cleanly.
- `KamikazeDebugHotkey.cs` — F8/F9 hotkeys for testing. Worth reading even if
  you don't ship debug hotkeys, because it shows how to inject a live gambit
  into a running game — useful pattern for any mod that wants to touch the
  active run.

---

## Building & installing

```bash
# Build all samples and write distributables to <repo>/Mods/
./build.sh

# Build, then also install into the live Gambonanza/Mods/ directory
./build.sh --install
```

Each mod ends up as a self-contained folder containing:

```
Mods/<ModName>/
  Gambonanza.<ModName>.dll
  mod.json
  <assets, if any>
```

To distribute one of your own mods, just zip its folder. Anyone with the
patched game can drop it into their `Mods/` directory and it Just Works on
next launch.

---

## Writing your own mod, end to end

1. Make a new folder under `sample_mods/` (or anywhere — these samples are
   just one possible layout).

2. Add a `mod.json`:
   ```json
   { "id": "HelloMod", "name": "Hello Mod", "version": "1.0.0",
     "author": "you", "entry": "HelloMod.HelloEntry", "enabled": true,
     "gameVersion": ">=1.0", "description": "Logs a friendly message." }
   ```

3. Add a `.csproj` (copy `SpeedMod.csproj` as a starting point — it already
   has the right reference paths into `../../refs/` and project references
   into `../../src/ModSdk/`).

4. Add a `.cs` file with a class implementing `IMod`:
   ```csharp
   using Gambonanza.ModSdk;
   using UnityEngine;
   namespace HelloMod
   {
       public sealed class HelloEntry : IMod
       {
           public void OnLoad(IModContext ctx)
           {
               Debug.Log("Hello from HelloMod!");
           }
       }
   }
   ```

5. Add it to `MODS=( ... )` in `build.sh` and run `./build.sh --install`.
   The game picks it up on next launch.

---

## Common gotchas

- **No Harmony, no MonoMod.** If you need to alter vanilla behaviour, use
  reflection to read/write the private state, or attach a `MonoBehaviour` to
  the live target and watch fields each frame. `GambitApi/CollectionPaginationPatch.cs`
  is the canonical reference for the latter.

- **Singletons may not exist yet.** Anything that touches
  `SingletonMonoBehaviour<T>.Instance` from `OnLoad` will throw — `OnLoad`
  fires from `GameManager.Start`, before most other singletons are up. Defer
  with a coroutine, a `MonoBehaviour`, or `Application.onBeforeRender`.

- **Resources next to your DLL.** `IModContext.ModDirectory` is the
  authoritative place to find sprites, configs, etc. you ship with your mod.
  `Path.Combine(ctx.ModDirectory, "myasset.png")` works on every platform.

- **Texture loading.** For PNGs, prefer `ModGambitApi.LoadSprite(path)` (in
  GambitApi) or roll your own `Texture2D.LoadImage`. Unity's built-in
  `Resources.Load` will not see files that aren't part of the game's asset
  bundles.

- **`Object.FindObjectsOfType` warnings.** The samples still use it in a few
  places. The newer `FindObjectsByType` is fine to swap in if you'd prefer to
  silence the warning.
