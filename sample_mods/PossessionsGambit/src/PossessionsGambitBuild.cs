using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

using System;
using Blukulele.Core;

namespace Gambonanza.PossessionsGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitPossession.cs.
    /// </summary>
    public sealed class PossessionsGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[PossessionsGambit] registering Possession's Gambit.");

            // Custom art: put `Possession.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "Possession.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit possession`, so keep it short and readable.
            var def = GambitBuilder.Create("possession")
                .WithName("Possession's Gambit")
                .WithDescription("Capturing with a <color=ß>PHANTOM</color> piece transforms it into a DEFAULT copy of the captured piece.")
                .WithRarity(Rarity.RARE)
                .WithFocus(Gambit_Focus.PHANTOM)
                .WithPrice(7)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach PossessionsGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitPossession>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[PossessionsGambit] registered '{def.Id}'.");
        }
    }
}
