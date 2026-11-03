using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.VulturesGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitVulture.cs.
    /// </summary>
    public sealed class VulturesGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[VulturesGambit] registering Vulture's Gambit.");

            // Custom art: put `Vulture.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "Vulture.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit vulture`, so keep it short and readable.
            var def = GambitBuilder.Create("vulture")
                .WithName("Vulture's Gambit")
                .WithDescription("Capturing a <color=◊>TRAPPED</color> piece <color=◊>TRAPS</color> every piece that was defending it.")
                .WithRarity(Rarity.EPIC)
                .WithFocus(Gambit_Focus.TRAP)
                .ShowTrap()
                .WithPrice(7)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach VulturesGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitVulture>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[VulturesGambit] registered '{def.Id}'.");
        }
    }
}
