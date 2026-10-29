using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.PoisonPawnsGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitPoisonPawn.cs.
    /// </summary>
    public sealed class PoisonPawnsGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[PoisonPawnsGambit] registering PoisonPawn's Gambit.");

            // Custom art: put `PoisonPawn.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "PoisonPawn.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit poison-pawn`, so keep it short and readable.
            var def = GambitBuilder.Create("poison-pawn")
                .WithName("Poison Pawn's Gambit")
                .WithDescription("If one of your <sprite=5> <color=&>PAWNS</color> is captured by a non <sprite=5> <color=&>PAWN</color> piece, skip the enemy's turn.")
                .WithRarity(Rarity.RARE)
                .WithFocus(Gambit_Focus.PAWN, Gambit_Focus.SACRIFICE)
                .WithPrice(7)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach PoisonPawnsGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitPoisonPawn>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[PoisonPawnsGambit] registered '{def.Id}'.");
        }
    }
}
