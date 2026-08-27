using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.TortoisesGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitTortoise.cs.
    /// </summary>
    public sealed class TortoisesGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[TortoisesGambit] registering Tortoise's Gambit.");

            // Custom art: put `Tortoise.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "Tortoise.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit tortoise`, so keep it short and readable.
            var def = GambitBuilder.Create("tortoise")
                .WithName("Tortoise's Gambit")
                // Select a random description for game startup
                .WithDescription("Every <sprite=9> <color=£>KING</color> move has a<br><color=Ø>1/2 chance</color> to also<br><wave><color=∫>COUNT AS</color></wave> <color=µ>WAITING</color>.")
                .WithRarity(Rarity.RARE)
                .WithFocus(Gambit_Focus.WAIT, Gambit_Focus.KING)
                .WithPrice(7)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                .ShowWait()
                .ShowConsideredAs()
                // This tells GambitApi to attach TortoisesGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitTortoise>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[TortoisesGambit] registered '{def.Id}'.");
        }
    }
}
