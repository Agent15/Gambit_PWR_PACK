# GambonanzaMods

A small, self-contained modding framework for the Steam game **Gambonanza**,
plus sample mods. The framework patches the game's `Assembly-CSharp.dll`
in-place to add three call sites, drops a runtime mod loader into the game's
`Managed/` directory, and from then on any mod is a normal .NET DLL dropped
into `Gambonanza/Mods/<ModName>/`.

This repo contains everything: the SDK, the loader, the in-game mod manager
UI, the patcher, and sample mods that double as documentation.

## Quick start

```bash
git clone https://github.com/bentrd/GambonanzaMods.git
cd GambonanzaMods
./build.sh
```

That's it. The script auto-detects your Gambonanza install, patches the
game, builds and installs the sample mods, and leaves you ready to
launch from Steam. Re-runnable any time — the patcher always works from a
backup of the original `Assembly-CSharp.dll`.

If auto-detection fails (non-default Steam library, custom install path),
pass the install path explicitly:

```bash
./build.sh "/some/path/to/Gambonanza"
GAMBONANZA_DIR="/some/path/to/Gambonanza" ./build.sh
```

To install only the framework without the sample mods:

```bash
./build.sh --skip-samples
```

**Requirements:** bash (Git Bash / WSL on Windows works), .NET SDK 8.0 or
newer, and an installed copy of Gambonanza on Steam.

## Layout

```
GambonanzaMods/
├── src/
│   ├── ModSdk/      Public API — IMod, IModContext, IModLifecycle.
│   ├── ModHost/     Runtime loader — discovers Mods/, parses mod.json,
│   │                Assembly.LoadFroms each DLL, dispatches lifecycle events.
│   ├── GameUI/      Pixel.* helpers for cloning game UI into mods.
│   └── Patcher/     Cecil-based one-shot patcher.
├── sample_mods/     Source for SpeedMod, GambitApi, custom gambits, overlays.
├── Mods/            Pre-built distributables — drop a subfolder into
│                    Gambonanza/Mods/ if you'd rather skip building.
├── docs/            UI_API.md (Pixel.* reference).
├── build.sh         Does everything end-to-end.
└── refs/            Auto-populated by build.sh on first run from your
                     own game's Managed/ folder. Not in version control —
                     these DLLs are copyrighted by Unity / Blukulele.
```

## How the framework works

The patcher injects exactly three calls into vanilla Gambonanza:

| Where in `Assembly-CSharp.dll`              | Hook                                  |
| ------------------------------------------- | ------------------------------------- |
| `Blukulele.Core.GameManager.Start`          | `ModHost.LoadAll()` — boot, mod loading, and console setup. |
| `Blukulele.CHE.CanvasMenu.OnEnable`         | `ModHost.OnHomeMenuOpenedInvoke(this)` — adds the CONSOLE home-screen button. |
| `Blukulele.CHE.SettingsCanvas.OnEnable`     | `ModHost.OnSettingsOpenedInvoke(this)` — fans out to mods that subscribed via `IModContext.OnSettingsOpened`. |

The in-game console opens with `F10`, `F1`, backtick, or the home-screen CONSOLE button. Type `help` to list commands.

Everything else lives in plain managed DLLs that get loaded via
`Assembly.LoadFrom`. There is no Harmony, no MonoMod runtime detour, no IL
weaving inside individual mods — only those three patches.

A marker class `__GambonanzaModHostPatched` is added to the patched assembly
so the patcher can detect a previous run and stay idempotent.

## Writing a mod

Read [sample_mods/README.md](sample_mods/README.md) and crib from `SpeedMod`
(the smallest sample) or `KamikazeGambit` (a feature mod that registers a
custom gambit via `GambitApi`).

The minimum viable mod is:

```csharp
public sealed class HelloMod : Gambonanza.ModSdk.IMod
{
    public void OnLoad(Gambonanza.ModSdk.IModContext ctx)
    {
        UnityEngine.Debug.Log("Hello from a mod!");
    }
}
```

…plus a six-line `mod.json` next to the compiled DLL.

While iterating on a mod, you don't need to re-run the full
`./build.sh` — `sample_mods/build.sh` rebuilds samples only and copies the
output into the game's `Mods/` with `--install`.

## License

MIT — see [LICENSE](LICENSE).
