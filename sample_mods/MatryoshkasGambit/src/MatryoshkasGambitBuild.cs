using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

using System;
using Blukulele.Core;

namespace Gambonanza.MatryoshkasGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitMatryoshka.cs.
    /// </summary>
    public sealed class MatryoshkasGambitBuild : IMod
    {
        public Sprite mySprite;
        public void OnLoad(IModContext context)
        {
            context.LogLine("[MatryoshkasGambit] registering Matryoshka's Gambit.");

            // Custom art: put `Matryoshka.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "Matryoshka.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit matryoshka`, so keep it short and readable.
            var def = GambitBuilder.Create("matryoshka")
                .WithName("Matryoshka's Gambit")
                // Select a random description for game startup
                .WithDescription("If any of your pieces is captured, earn a piece of the next highest value. <br>(<sprite=10>><sprite=7>><sprite=6>><sprite=8>><sprite=9>><sprite=5>)")
                .WithRarity(Rarity.LEGENDARY)
                .WithFocus(Gambit_Focus.SACRIFICE)
                .WithPrice(10)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach MatryoshkasGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitMatryoshka>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[MatryoshkasGambit] registered '{def.Id}'.");
        }
    }
}
