using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.HydraulicPresssGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitHydraulicPress.cs.
    /// </summary>
    public sealed class HydraulicPresssGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[HydraulicPresssGambit] registering HydraulicPress's Gambit.");

            // Custom art: put `HydraulicPress.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "HydraulicPress.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit hydraulic-press`, so keep it short and readable.
            var def = GambitBuilder.Create("hydraulic-press")
                .WithName("Hydraulic Press's Gambit")
                .WithDescription("Capturing with a <sprite=6> <color=°>ROOK</color> on an <bounce><color=≠>ADJACENT</color></bounce> tile skips the enemy's turn.")
                .WithRarity(Rarity.EPIC)
                .WithFocus(Gambit_Focus.ROOK)
                .WithPrice(9)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach HydraulicPresssGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitHydraulicPress>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[HydraulicPresssGambit] registered '{def.Id}'.");
        }        
    }
}
