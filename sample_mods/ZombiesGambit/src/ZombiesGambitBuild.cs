using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.ZombiesGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitZombie.cs.
    /// </summary>
    public sealed class ZombiesGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[ZombiesGambit] registering Zombie's Gambit.");

            // Custom art: put `Zombie.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "Zombie.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit zombie`, so keep it short and readable.
            var def = GambitBuilder.Create("zombie")
                .WithName("Zombie's Gambit")
                // Select a random description for game startup
                .WithDescription("Capturing with a <sprite=9> <color=£>KING</color> earns a <sprite=9> <color=£>KING</color>")
                .WithRarity(Rarity.EPIC)
                .WithFocus(Gambit_Focus.KING)
                .WithPrice(10)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach ZombiesGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitZombie>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[ZombiesGambit] registered '{def.Id}'.");
        }  
    }
}
