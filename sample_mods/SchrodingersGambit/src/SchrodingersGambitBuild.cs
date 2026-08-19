using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

using System;
using Blukulele.Core;

namespace Gambonanza.SchrodingersGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitSchrodinger.cs.
    /// </summary>
    public sealed class SchrodingersGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[SchrodingersGambit] registering Schrodinger's Gambit.");

            // Custom art: put `Schrodinger.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "Schrodinger.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit schrodinger`, so keep it short and readable.
            var def = GambitBuilder.Create("schrodinger")
                .WithName("Schrödinger's Gambit")
                // Select a random description for game startup
                .WithDescription("If any of your <color=ß>PHANTOM</color> pieces is <wave><sprite=11> THREATENED</wave>, it becomes DEFAULT.")
                .WithRarity(Rarity.RARE)
                .WithFocus(Gambit_Focus.PHANTOM)
                .WithPrice(6)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach SchrodingersGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitSchrodinger>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[SchrodingersGambit] registered '{def.Id}'.");
        }  
    }
}
