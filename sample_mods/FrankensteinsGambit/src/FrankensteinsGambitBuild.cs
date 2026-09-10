using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;
using Blukulele.Core;

namespace Gambonanza.FrankensteinsGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitFrankenstein.cs.
    /// </summary>
    public sealed class FrankensteinsGambitBuild : IMod
    {
        public Sprite mySprite;

        public static string DefaultDescription = "After 3 of your pieces are captured, earn a random piece among those 3.";

        public void OnLoad(IModContext context)
        {
            context.LogLine("[FrankensteinsGambit] registering Frankenstein's Gambit.");

            // Custom art: put `Frankenstein.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "Frankenstein.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit frankenstein`, so keep it short and readable.
            var def = GambitBuilder.Create("frankenstein")
                .WithName("Frankenstein's Gambit")
                .WithDescription(DefaultDescription)
                .WithRarity(Rarity.COMMON)
                .WithFocus(Gambit_Focus.SACRIFICE)
                .WithPrice(5)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach FrankensteinsGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitFrankenstein>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();

            context.LogLine($"[FrankensteinsGambit] registered '{def.Id}'.");
        }

        // GambitFrankenstein will use this to update the gambit's description
        public static void UpdateDescription(PieceType[] types = null)
        {
            // Define an array of in-game sprites to match with the values of PieceType
            string[] stringTypes = {
                "<sprite=5>",
                "<sprite=6>",
                "<sprite=7>",
                "<sprite=8>",
                "<sprite=10>",
                "<sprite=9>",
                "-",
            };
            string desc = DefaultDescription;
            if(types is not null && types.Length >= 3)
            {
                desc += $"<br>( {stringTypes[(int)types[0]]} , {stringTypes[(int)types[1]]} , {stringTypes[(int)types[2]]} )";
            }
            var locManager = SingletonMonoBehaviour<LocalizationManager>.Instance;
            if (locManager == null)
                return;

            var traduction = locManager.GetTraduction();
            if (traduction == null)
                return;

            var gambitNode = traduction["gambit"];
            if (gambitNode == null)
                return;
            
            gambitNode["frankenstein_description"] = desc;
        }  
    }
}
