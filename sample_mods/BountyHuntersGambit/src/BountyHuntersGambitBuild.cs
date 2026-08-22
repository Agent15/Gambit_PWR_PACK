using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

using System;
using Blukulele.Core;

namespace Gambonanza.BountyHuntersGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitBountyHunter.cs.
    /// </summary>
    public sealed class BountyHuntersGambitBuild : IMod
    {
        public Sprite mySprite;

        // Define an array of strings to translate a PieceType into formatted description text
        // Each value of PieceType has a numerical value which is implied by each items position in this list.
        private static string[] translations = {
            "<sprite=5> <color=&>PAWN</color>",
            "<sprite=6> <color=°>ROOK</color>",
            "<sprite=7> <color=|>KNIGHT</color>",
            "<sprite=8> <color=∏>BISHOP</color>",
            "<sprite=10> <color=^>QUEEN</color>",
            "<sprite=9> <color=£>KING</color>"
        };

        public void OnLoad(IModContext context)
        {
            context.LogLine("[BountyHuntersGambit] registering Bounty Hunter's Gambit.");

            // Custom art: put `BountyHunter.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "BountyHunter.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit bounty-hunter`, so keep it short and readable.
            var def = GambitBuilder.Create("bounty-hunter")
                .WithName("Bounty Hunter's Gambit")
                // Select a random description for game startup
                .WithDescription("Capturing a <sprite=5> <color=&>PAWN</color> earns <color=*>$5</color>.<br><i>(Changes after every trigger)</i>")
                .WithRarity(Rarity.RARE)
                .WithFocus(Gambit_Focus.MONEY)
                .WithPrice(6)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach BountyHuntersGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitBountyHunter>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[BountyHuntersGambit] registered '{def.Id}'.");
        }

        // The vanilla game stores its gambit titles and descriptions in the resources.assets file.
        // This method overwrites that file at the index "bounty-hunter_description"
        // Credit to Bentrd for laying the groundwork in GambitRegistry
        public static void UpdateDescription(PieceType type)
        {
            var locManager = SingletonMonoBehaviour<LocalizationManager>.Instance;
            if (locManager == null)
            {
                Debug.LogWarning("[GambitApi] LocalizationManager not found, tooltip text will be empty.");
                return;
            }

            // Force load if not cached
            var traduction = locManager.GetTraduction();
            if (traduction == null)
            {
                Debug.LogWarning("[GambitApi] GetTraduction() returned null.");
                return;
            }

            var gambitNode = traduction["gambit"];
            if (gambitNode == null)
            {
                Debug.LogWarning("[GambitApi] traduction['gambit'] node not found.");
                return;
            }

            gambitNode[$"bounty-hunter_description"] = $"Capturing a {translations[(int) type]} earns <color=*>$5</color>.<br><i>(Changes after every trigger)</i>";
        }  
    }
}
