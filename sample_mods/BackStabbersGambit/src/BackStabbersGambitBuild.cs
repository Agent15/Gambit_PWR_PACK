using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

using System;
using Blukulele.Core;

namespace Gambonanza.BackStabbersGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitBackStabber.cs.
    /// </summary>
    public sealed class BackStabbersGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[BackStabbersGambit] registering BackStabber's Gambit.");

            // Custom art: put `BackStabber.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "BackStabber.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit back-stabber`, so keep it short and readable.
            var def = GambitBuilder.Create("back-stabber")
                .WithName("Back-Stabber's Gambit")
                .WithDescription("Capturing with a backward move skips the enemy's turn.")
                .WithRarity(Rarity.EPIC)
                .WithFocus(Gambit_Focus.NONE)
                .WithPrice(10)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach BackStabbersGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitBackStabber>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[BackStabbersGambit] registered '{def.Id}'.");
        }
    }
}
