using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

using System;
using Blukulele.Core;

namespace Gambonanza.NullsGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitNull.cs.
    /// </summary>
    public sealed class NullsGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[NullsGambit] registering Null's Gambit.");

            // Custom art: put `Null.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "Null.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit null`, so keep it short and readable.
            var def = GambitBuilder.Create("null")
                .WithName("Null's Gambit")
                .WithDescription("__________")
                .WithRarity(Rarity.STRAIN)
                .WithFocus(Gambit_Focus.NONE)
                .WithPrice(1)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach NullsGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitNull>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[NullsGambit] registered '{def.Id}'.");
        }
    }
}
