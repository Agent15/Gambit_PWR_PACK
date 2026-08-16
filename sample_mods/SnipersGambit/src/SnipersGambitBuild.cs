using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.SnipersGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitSniper.cs.
    /// </summary>
    public sealed class SnipersGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[SnipersGambit] registering Sniper's Gambit.");

            // Custom art: put `Sniper.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "Sniper.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit sniper`, so keep it short and readable.
            var def = GambitBuilder.Create("sniper")
                .WithName("Sniper's Gambit")
                //Select a random description for this render
                .WithDescription("Capturing with a <sprite=8> <color=∏>BISHOP</color> from 3 or more spaces away skips the enemy's turn.")
                .WithRarity(Rarity.EPIC)
                .WithFocus(Gambit_Focus.NONE)
                .WithPrice(8)
                .WithVisual(sprite)
                .WithVisualScale(0.9f)
                // This tells GambitApi to attach SnipersGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitSniper>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[SnipersGambit] registered '{def.Id}'.");
        }        
    }
}
