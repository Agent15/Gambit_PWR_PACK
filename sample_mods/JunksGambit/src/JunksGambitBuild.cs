using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

using System;
using Blukulele.Core;

namespace Gambonanza.JunksGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitJunk.cs.
    /// </summary>
    public sealed class JunksGambitBuild : IMod
    {
        public Sprite mySprite;

        //Initialize a randomizer and the set of all possible descriptions
        private System.Random pick = new();
        public static string[] descriptions = {
            "Your <sprite=9> <color=£>KINGS</color> also move like <sprite=5> <color=&>PAWNS</color>.", 
            "Your <sprite=10> <color=^>QUEENS</color> also move like <sprite=5> <color=&>PAWNS</color>.",
            "Your <sprite=10> <color=^>QUEENS</color> also move like <sprite=9> <color=£>KINGS</color>.", 
            "Your <sprite=10> <color=^>QUEENS</color> also move like <sprite=6> <color=°>ROOKS</color>.", 
            "Your <sprite=10> <color=^>QUEENS</color> also move like <sprite=8> <color=∏>BISHOPS</color>.", 
            "When one of your pieces moves onto a <sprite=0> <color=ƒ>BLESSED TILE</color>, it gets <color=ƒ>BLESSED</color>.",
            "When one of your pieces moves onto a <sprite=2> <color=æ>PROTECTIVE TILE</color>, it gets <color=æ>PROTECTED</color>.",
            "When one of your pieces moves onto a <sprite=1> <color=∞>GOLDEN TILE</color>, it turns <color=∞>GOLD</color>.",
            "When one of your pieces moves onto a <sprite=12> <color=ß>PHANTOM TILE</color>, grant a <color=ß>PHANTOM</color> copy of that piece.",
            "When an enemy piece moves onto a <sprite=3> <color=◊>TRAP TILE</color>, it gets <color=◊>TRAPED</color>.",
            "Opening with (d4, d5, c4, dxc4, e4) sacrifices a <sprite=5> <color=&>PAWN</color>, but gives you a firm hold on the center.",
            "Buying a <sprite=5> <color=&>PAWN</color> in the shop grants a <sprite=5> <color=&>PAWN</color>.",
            "Buying a <sprite=9> <color=£>KING</color> in the shop grants a <sprite=9> <color=£>KING</color>.",
            "Buying a <sprite=8> <color=∏>BISHOP</color> in the shop grants a <sprite=8> <color=∏>BISHOP</color>.",
            "Buying a <sprite=7> <color=|>KNIGHT</color> in the shop grants a <sprite=7> <color=|>KNIGHT</color>.",
            "Buying a <sprite=6> <color=°>ROOK</color> in the shop grants a <sprite=6> <color=°>ROOK</color>.",
            "Buying a <sprite=10> <color=^>QUEEN</color> in the shop grants a <sprite=10> <color=^>QUEENS</color>.",
            "Buying a piece token grants a random piece.",
            "Buying a gambit token grants a random gambit.",
            "Buying a tile token modifies a tile of your choice to a random modifier.<br>(<sprite=0> <sprite=1> <sprite=2> <sprite=3> <sprite=12>)",
            "<color=©>LANDING</color> a piece removes it from the stock and adds it to the board.",
            "<color=µ>WAITING</color> skips the player turn.",
            "Moving a <sprite=5> <color=&>PAWN</color> to the enemy's back rank <rainb l=0.5>PROMOTES</rainb> it.",
            "Not moving a <wave><sprite=11> THREATENED</wave> piece has a <color=Ø>1/1 chance</color> of it getting captured",
            "Durring <shake>Crumble Mode</shake>, multiple tiles will fall efter every turn."            
        };

        public void OnLoad(IModContext context)
        {
            context.LogLine("[JunksGambit] registering Junk's Gambit.");

            InjectDescriptions();

            // Custom art: put `Junk.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "Junk.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit junk`, so keep it short and readable.
            var def = GambitBuilder.Create("junk")
                .WithName("Junk's Gambit")
                //Select a random description on game startup
                .WithDescription(descriptions[pick.Next(descriptions.Length)])
                .WithRarity(Rarity.COMMON)
                .WithFocus(Gambit_Focus.UTILITY)
                .WithPrice(1)
                .WithVisual(sprite)
                .WithVisualScale(0.9f)
                // This tells GambitApi to attach JunksGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitJunk>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[JunksGambit] registered '{def.Id}'.");
        }

        // The vanilla game stores its gambit titles and descriptions in resources.assets
        // This method will add the list of this gambit's descriptions to that file
        private static void InjectDescriptions()
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

            // The JSON implementation uses custom setters via indexer
            gambitNode["junk2_description"] = "I changed!!!";

            for(int i = 0; i < descriptions.Length; i++)
            {
                gambitNode[$"junk{i}_description"] = descriptions[i];
            }
        }     
    }
}
