using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.SkeletonsGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitSkeleton.cs.
    /// </summary>
    public sealed class SkeletonsGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[SkeletonsGambit] registering Skeleton's Gambit.");

            // Custom art: put `Skeleton.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "Skeleton.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit skeleton`, so keep it short and readable.
            var def = GambitBuilder.Create("skeleton")
                .WithName("Skeleton's Gambit")
                .WithDescription("If you only have 1 piece on the board, <color=Ø>1/3 chance</color> to <color=æ>PROTECT</color> it each turn.")
                .WithRarity(Rarity.RARE)
                .WithFocus(Gambit_Focus.PROTECTIVE)
                .WithPrice(8)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach SkeletonsGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitSkeleton>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[SkeletonsGambit] registered '{def.Id}'.");
        }
    }
}
