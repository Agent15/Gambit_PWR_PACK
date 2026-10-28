using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.MoneyTreesGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitMoneyTree.cs.
    /// </summary>
    public sealed class MoneyTreesGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[MoneyTreesGambit] registering MoneyTree's Gambit.");

            // Custom art: put `MoneyTree.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "MoneyTree.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit money-tree`, so keep it short and readable.
            var def = GambitBuilder.Create("money-tree")
                .WithName("MoneyTree's Gambit")
                .WithDescription("At the end of a game, earn <color=*>$1</color> (Increases by <color=*>+$1</color> each game).")
                .WithRarity(Rarity.COMMON)
                .WithFocus(Gambit_Focus.MONEY)
                .WithPrice(5)
                .WithVisual(sprite)
                .WithVisualScale(0.9f)
                // This tells GambitApi to attach MoneyTreesGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitMoneyTree>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[MoneyTreesGambit] registered '{def.Id}'.");
        }
    }
}
