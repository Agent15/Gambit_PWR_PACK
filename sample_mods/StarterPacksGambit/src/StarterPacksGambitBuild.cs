using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

using System;
using Blukulele.Core;

namespace Gambonanza.StarterPacksGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitStarterPack.cs.
    /// </summary>
    public sealed class StarterPacksGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[StarterPacksGambit] registering StarterPack's Gambit.");

            // Custom art: put `StarterPack.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "StarterPack.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit starter-pack`, so keep it short and readable.
            var def = GambitBuilder.Create("starter-pack")
                .WithName("Starter Pack's Gambit")
                .WithDescription("Buying a gambit in the shop also earns a piece of its respective synergy <br><i>(if applicable)</i>")
                .WithRarity(Rarity.COMMON)
                .WithFocus(Gambit_Focus.UTILITY)
                .WithPrice(5)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach StarterPacksGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitStarterPack>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[StarterPacksGambit] registered '{def.Id}'.");
        } 
    }
}
