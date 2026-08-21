using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.EnPassantsGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitEnPassant.cs.
    /// </summary>
    public sealed class EnPassantsGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[EnPassantsGambit] registering EnPassant's Gambit.");

            // Custom art: put `EnPassant.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "EnPassant.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit en-passant`, so keep it short and readable.
            var def = GambitBuilder.Create("en-passant")
                .WithName("En Passant's Gambit")
                //Select a random description for this render
                .WithDescription("You can capture the last enemy piece to move by moving a piece over its original tile.")
                .WithRarity(Rarity.EPIC)
                .WithFocus(Gambit_Focus.UTILITY)
                .WithPrice(8)
                .WithVisual(sprite)
                .WithVisualScale(0.9f)
                // This tells GambitApi to attach EnPassantsGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitEnPassant>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[EnPassantsGambit] registered '{def.Id}'.");
        }        
    }
}
