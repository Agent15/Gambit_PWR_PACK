using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

using System;
using Blukulele.Core;

namespace Gambonanza.GuardianAngelsGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitGuardianAngel.cs.
    /// </summary>
    public sealed class GuardianAngelsGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[GuardianAngelsGambit] registering Guardian Angel's Gambit.");

            // Custom art: put `GuardianAngel.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "GuardianAngel.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit guardian-angel`, so keep it short and readable.
            var def = GambitBuilder.Create("guardian-angel")
                .WithName("Guardian Angel's Gambit")
                // Select a random description for game startup
                .WithDescription("If any of your pieces is <color=æ>PROTECED</color>, <color=ƒ>BLESS</color> it when the effect expires.")
                .WithRarity(Rarity.RARE)
                .WithFocus(Gambit_Focus.PROTECTIVE, Gambit_Focus.BLESS)
                .WithPrice(7)
                .WithVisual(sprite)
                .WithVisualScale(1.2f)
                .ShowBless()
                .ShowProtect()
                // This tells GambitApi to attach GuardianAngelsGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitGuardianAngel>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[GuardianAngelsGambit] registered '{def.Id}'.");
        }
    }
}
