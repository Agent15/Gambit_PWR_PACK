using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

using System;
using Blukulele.Core;

namespace Gambonanza.FishingNetsGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitFishingNet.cs.
    /// </summary>
    public sealed class FishingNetsGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[FishingNetsGambit] registering FishingNet's Gambit.");

            // Custom art: put `FishingNet.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "FishingNet.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit fishing-net`, so keep it short and readable.
            var def = GambitBuilder.Create("fishing-net")
                .WithName("Fishing Net's Gambit")
                // Select a random description for game startup
                .WithDescription("When an enemy piece is <color=◊>TRAPPED</color>, skip the enemy's turn.")
                .WithRarity(Rarity.RARE)
                .WithFocus(Gambit_Focus.TRAP)
                .WithPrice(6)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach FishingNetsGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitFishingNet>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[FishingNetsGambit] registered '{def.Id}'.");
        } 
    }
}
