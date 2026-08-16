using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

using System;
using Blukulele.Core;

namespace Gambonanza.CrybabysGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitCrybaby.cs.
    /// </summary>
    public sealed class CrybabysGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[CrybabysGambit] registering Crybaby's Gambit.");

            // Custom art: put `Crybaby.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "Crybaby.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit crybaby`, so keep it short and readable.
            var def = GambitBuilder.Create("crybaby")
                .WithName("Crybaby's Gambit")
                // Select a random description for game startup
                .WithDescription("When any of your pieces is <wave><sprite=11> THREATENED</wave>, it is also <wave><color=∫>COUNTED AS</color></wave> a capture.")
                .ShowConsideredAs()
                .WithRarity(Rarity.RARE)
                .WithFocus(Gambit_Focus.SACRIFICE)
                .WithPrice(7)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach CrybabysGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitCrybaby>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();
                
            context.LogLine($"[CrybabysGambit] registered '{def.Id}'.");
        }  
    }
}
