using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.LinemansGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitLineman.cs.
    /// </summary>
    public sealed class LinemansGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[LinemansGambit] registering Lineman's Gambit.");

            // Custom art: put `Lineman.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "Lineman.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit lineman`, so keep it short and readable.
            var def = GambitBuilder.Create("lineman")
                .WithName("Lineman's Gambit")
                .WithDescription("<color=æ>PROTECT</color> all your<br><sprite=5> <color=&>PAWNS</color> with an enemy<br><sprite=5> <color=&>PAWN</color> directly in front of them.")
                .WithRarity(Rarity.EPIC)
                .WithFocus(Gambit_Focus.PAWN, Gambit_Focus.PROTECTIVE)
                .WithPrice(9)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach LinemansGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitLineman>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[LinemansGambit] registered '{def.Id}'.");
        }
    }
}
