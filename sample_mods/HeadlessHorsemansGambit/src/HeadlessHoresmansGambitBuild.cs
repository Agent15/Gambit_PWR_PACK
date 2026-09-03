using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

using System;
using Blukulele.Core;

namespace Gambonanza.HeadlessHorsemansGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitHeadlessHorseman.cs.
    /// </summary>
    public sealed class HeadlessHorsemansGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[HeadlessHorsemansGambit] registering HeadlessHorseman's Gambit.");

            // Custom art: put `HeadlessHorseman.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "HeadlessHorseman.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit headless-horseman`, so keep it short and readable.
            var def = GambitBuilder.Create("headless-horseman")
                .WithName("HeadlessHorseman's Gambit")
                .WithDescription("Moving a <sprite=9> <color=£>KING</color> <bounce><color=≠>ADJACENT</color></bounce> to a <sprite=7> <color=|>KNIGHT</color> (or vice versa) earns <color=*>$5</color>.<br><i>(Once per game)</i>")
                .WithRarity(Rarity.COMMON)
                .WithFocus(Gambit_Focus.KING, Gambit_Focus.KNIGHT)
                .WithPrice(6)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach HeadlessHorsemansGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitHeadlessHorseman>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[HeadlessHorsemansGambit] registered '{def.Id}'.");
        }
    }
}
