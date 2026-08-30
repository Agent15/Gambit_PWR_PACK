using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

using System;
using Blukulele.Core;

namespace Gambonanza.EvangelistsGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitEvangelist.cs.
    /// </summary>
    public sealed class EvangelistsGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[EvangelistsGambit] registering Evangelist's Gambit.");

            // Custom art: put `Evangelist.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "Evangelist.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit evangelist`, so keep it short and readable.
            var def = GambitBuilder.Create("evangelist")
                .WithName("Evangelist's Gambit")
                .WithDescription("Moving a <sprite=8> <color=∏>BISHOP</color> <bounce><color=≠>ADJACENT</color></bounce> to an enemy piece has a <br><color=Ø>1/4 chance</color> to transform it into one of your pieces. <br><i>(If possible)</i>")
                .WithRarity(Rarity.EPIC)
                .WithFocus(Gambit_Focus.BISHOP)
                .WithPrice(8)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach EvangelistsGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitEvangelist>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();
                
            context.LogLine($"[EvangelistsGambit] registered '{def.Id}'.");
        } 
    }
}
