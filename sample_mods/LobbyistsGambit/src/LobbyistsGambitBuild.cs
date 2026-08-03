using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.LobbyistsGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitLobbyist.cs.
    /// </summary>
    public sealed class LobbyistsGambitBuild : IMod
    {
        public Sprite mySprite;

        public void OnLoad(IModContext context)
        {
            context.LogLine("[LobbyistsGambit] registering Lobbyist's Gambit.");

            // Custom art: put `Lobbyist.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "Lobbyist.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit lobbyist`, so keep it short and readable.
            var def = GambitBuilder.Create("lobbyist")
                .WithName("Lobbyist's Gambit")
                //Select a random description for this render
                .WithDescription("Seeling a <sprite=9> <color=£>KING</color> captures a random enemy piece.")
                .WithRarity(Rarity.EPIC)
                .WithFocus(Gambit_Focus.PIECE_SELLER)
                .WithPrice(1)
                .WithVisual(sprite)
                .WithVisualScale(0.9f)
                // This tells GambitApi to attach LobbyistsGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitLobbyist>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[LobbyistsGambit] registered '{def.Id}'.");
        }        
    }
}
