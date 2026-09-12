using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

using System;
using Blukulele.Core;

namespace Gambonanza.InternsGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitIntern.cs.
    /// </summary>
    public sealed class InternsGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[InternsGambit] registering Intern's Gambit.");

            // Custom art: put `Intern.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "Intern.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit intern`, so keep it short and readable.
            var def = GambitBuilder.Create("intern")
                .WithName("Intern's Gambit")
                .WithDescription("Enemy <sprite=5> <color=&>PAWNS</color> instead promote to random pieces.")
                .WithRarity(Rarity.COMMON)
                .WithFocus(Gambit_Focus.UTILITY)
                .WithPrice(5)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach InternsGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitIntern>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[InternsGambit] registered '{def.Id}'.");
        }
    }
}
