using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.JudgementsGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitJudgement.cs.
    /// </summary>
    public sealed class JudgementsGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[JudgementsGambit] registering Judgement's Gambit.");

            // Custom art: put `Judgement.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "Judgement.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit judgement`, so keep it short and readable.
            var def = GambitBuilder.Create("judgement")
                .WithName("Judgement's Gambit")
                .WithDescription("If a <color=ƒ>BLESSED</color> piece is captured, cause the captured tile to <shake>Crumble</shake>.")
                .ShowBless()
                .WithRarity(Rarity.EPIC)
                .WithFocus(Gambit_Focus.BLESS, Gambit_Focus.CRUMBLE)
                .WithPrice(9)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach JudgementsGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitJudgement>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[JudgementsGambit] registered '{def.Id}'.");
        }
    }
}
