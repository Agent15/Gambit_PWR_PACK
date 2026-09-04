using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

using System;
using Blukulele.Core;

namespace Gambonanza.VampiresGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitVampire.cs.
    /// </summary>
    public sealed class VampiresGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[VampiresGambit] registering Vampire's Gambit.");

            // Custom art: put `Vampire.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "Vampire.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit vampire`, so keep it short and readable.
            var def = GambitBuilder.Create("vampire")
                .WithName("Vampire's Gambit")
                // Select a random description for game startup
                .WithDescription("Immediately turns <color=∞>GOLDEN</color> pieces DEFAULT and earns <color=∞>$2</color> for each of them")
                .WithRarity(Rarity.RARE)
                .WithFocus(Gambit_Focus.GOLDEN)
                .WithPrice(7)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach VampiresGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitVampire>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[VampiresGambit] registered '{def.Id}'.");
        }  
    }
}
