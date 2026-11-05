using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.BulletTimesGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitBulletTime.cs.
    /// </summary>
    public sealed class BulletTimesGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[BulletTimesGambit] registering BulletTime's Gambit.");

            // Custom art: put `BulletTime.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "BulletTime.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit bullet-time`, so keep it short and readable.
            var def = GambitBuilder.Create("bullet-time")
                .WithName("BulletTime's Gambit")
                .WithDescription("Skipping the enemy's turn also prevents crumbling tiles from falling.")
                .WithRarity(Rarity.RARE)
                .WithFocus(Gambit_Focus.CRUMBLE)
                .WithPrice(1)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach BulletTimesGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitBulletTime>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[BulletTimesGambit] registered '{def.Id}'.");
        }
    }
}
