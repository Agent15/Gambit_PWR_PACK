using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.DootsGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitDoot.cs.
    /// </summary>
    public sealed class DootsGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[DootsGambit] registering Doot's Gambit.");

            // Custom art: put `Doot.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "Doot.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit doot`, so keep it short and readable.
            var def = GambitBuilder.Create("doot")
                .WithName("Doot's Gambit")
                .WithDescription("Moving <bounce><color=≠>ADJACENT</color></bounce> to an enemy piece has a <color=Ø>1/7 chance</color> to capture it.")
                .WithRarity(Rarity.COMMON)
                .WithFocus(Gambit_Focus.NONE)
                .WithPrice(5)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach DootsGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitDoot>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[DootsGambit] registered '{def.Id}'.");
        }
    }
}
