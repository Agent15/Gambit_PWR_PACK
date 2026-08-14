using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

using System;
using Blukulele.Core;

namespace Gambonanza.DominoEffectsGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitDominoEffect.cs.
    /// </summary>
    public sealed class DominoEffectsGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[DominoEffectsGambit] registering Domino Effect's Gambit.");

            // Custom art: put `DominoEffect.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "DominoEffect.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit domino-effect`, so keep it short and readable.
            var def = GambitBuilder.Create("domino-effect")
                .WithName("Domino Effect's Gambit")
                // Select a random description for game startup
                .WithDescription("When an enemy piece is <color=◊>TRAPPED</color>, <color=◊>TRAP</color> every enemy piece <bounce><color=≠>ADJACENT</color></bounce> to it.")
                .WithRarity(Rarity.EPIC)
                .WithFocus(Gambit_Focus.TRAP)
                .WithPrice(8)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach DominoEffectsGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitDominoEffect>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[DominoEffectsGambit] registered '{def.Id}'.");
        }
    }
}
