using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.BougiesGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitBougie.cs.
    /// </summary>
    public sealed class BougiesGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[BougiesGambit] registering Bougie's Gambit.");

            // Custom art: put `Bougie.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "Bougie.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit bougie`, so keep it short and readable.
            var def = GambitBuilder.Create("bougie")
                .WithName("Bougie's Gambit")
                //Select a random description for this render
                .WithDescription("Every time you spend money, earn <color=*>$1</color>.")
                .WithRarity(Rarity.RARE)
                .WithFocus(Gambit_Focus.UTILITY)
                .WithPrice(6)
                .WithVisual(sprite)
                .WithVisualScale(0.9f)
                // This tells GambitApi to attach BougiesGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitBougie>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[BougiesGambit] registered '{def.Id}'.");
        }        
    }
}
