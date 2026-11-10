using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.PrismsGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitPrism.cs.
    /// </summary>
    public sealed class PrismsGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[PrismsGambit] registering Prism's Gambit.");

            // Custom art: put `Prism.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "Prism.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit prism`, so keep it short and readable.
            var def = GambitBuilder.Create("prism")
                .WithName("Prism's Gambit")
                .WithDescription("After modifying a tile,<br>(<sprite=0> <sprite=1> <sprite=2> <sprite=3> <sprite=12>)<br> modify another random tile.")
                .WithRarity(Rarity.RARE)
                .WithFocus(Gambit_Focus.PROTECTIVE, Gambit_Focus.BLESS, Gambit_Focus.TRAP, Gambit_Focus.GOLDEN, Gambit_Focus.PHANTOM)
                .WithPrice(8)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach PrismsGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitPrism>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[PrismsGambit] registered '{def.Id}'.");
        }
    }
}
