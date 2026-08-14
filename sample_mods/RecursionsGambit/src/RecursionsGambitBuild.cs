using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.RecursionsGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitRecursion.cs.
    /// </summary>
    public sealed class RecursionsGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[RecursionsGambit] registering Recursion's Gambit.");

            // Custom art: put `Recursion.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "Recursion.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit recursion`, so keep it short and readable.
            var def = GambitBuilder.Create("recursion")
                .WithName("Recursion's Gambit")
                //Select a random description for this render
                .WithDescription("Moving the same piece, the same distance, in the same direction as your last move skips the enemy turn.<br><wave><sprite=11>WIP: Inconsistent<sprite=11></wave>")
                .WithRarity(Rarity.EPIC)
                .WithFocus(Gambit_Focus.NONE)
                .WithPrice(8)
                .WithVisual(sprite)
                .WithVisualScale(0.9f)
                // This tells GambitApi to attach RecursionsGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitRecursion>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[RecursionsGambit] registered '{def.Id}'.");
        }        
    }
}
