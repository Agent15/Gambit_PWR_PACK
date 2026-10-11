using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.TransylvaniasGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitTransylvania.cs.
    /// </summary>
    public sealed class TransylvaniasGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[TransylvaniasGambit] registering Transylvania's Gambit.");

            // Custom art: put `Transylvania.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "Transylvania.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit transylvania`, so keep it short and readable.
            var def = GambitBuilder.Create("transylvania")
                .WithName("Transylvania's Gambit")
                .WithDescription("Selling this gambit earns 3 random Halloween-themed gambits.")
                .WithRarity(Rarity.LEGENDARY)
                .WithFocus(Gambit_Focus.UTILITY)
                .WithPrice(12)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach TransylvaniasGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitTransylvania>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[TransylvaniasGambit] registered '{def.Id}'.");
        }
    }
}
