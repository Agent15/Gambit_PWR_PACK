using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.SpidersGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitSpider.cs.
    /// </summary>
    public sealed class SpidersGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[SpidersGambit] registering Spider's Gambit.");

            // Custom art: put `Spider.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "Spider.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit spider`, so keep it short and readable.
            var def = GambitBuilder.Create("spider")
                .WithName("Spider's Gambit")
                .WithDescription("After moving a <sprite=7> <color=|>KNIGHT</color>, <color=◊>TRAP</color>, every enemy piece it can capture.")
                .ShowTrap()
                .WithRarity(Rarity.EPIC)
                .WithFocus(Gambit_Focus.KNIGHT, Gambit_Focus.TRAP)
                .WithPrice(11)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach SpidersGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitSpider>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[SpidersGambit] registered '{def.Id}'.");
        }
    }
}
