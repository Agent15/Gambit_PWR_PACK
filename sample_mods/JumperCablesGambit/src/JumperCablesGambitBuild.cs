using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.JumperCablesGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitJumperCable.cs.
    /// </summary>
    public sealed class JumperCablesGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[JumperCablesGambit] registering Jumper Cables' Gambit.");

            // Custom art: put `JumperCable.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "JumperCables.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit jumper-cable`, so keep it short and readable.
            var def = GambitBuilder.Create("jumper-cable")
                .WithName("Jumper Cables' Gambit")
                //Select a random description for this render
                .WithDescription("After every move,<br><color=Ø> 1/5 chance</color> to trigger one of your gambits unconditionally")
                .WithRarity(Rarity.EPIC)
                .WithFocus(Gambit_Focus.PIECE_SELLER)
                .WithPrice(8)
                .WithVisual(sprite)
                .WithVisualScale(0.9f)
                // This tells GambitApi to attach JumperCablesGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitJumperCables>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[JumperCablesGambit] registered '{def.Id}'.");
        }        
    }
}
