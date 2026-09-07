using System.IO;
using Blukulele.CHE;
using Gambonanza.GambitApi;
using Gambonanza.ModSdk;
using UnityEngine;

using System;
using Blukulele.Core;

namespace Gambonanza.MysterysGambit
{
    /// <summary>
    /// Mod entry point. ModHost creates this class from mod.json and calls OnLoad.
    ///
    /// This file is only responsible for registering the card/gambit definition:
    /// name, tooltip, rarity, price, art, and which runtime behaviour to attach.
    /// The actual gameplay logic is in GambitMystery.cs.
    /// </summary>
    public sealed class MysterysGambitBuild : IMod
    {
        public Sprite mySprite;

        public static string defaultDescription = "Mimics the effects of 3 random gambits<br><i>(Including modded gambits)</i>";

        public void OnLoad(IModContext context)
        {
            context.LogLine("[MysterysGambit] registering Mystery's Gambit.");

            // Custom art: put `Mystery.png` next to mod.json.
            var spritePath = Path.Combine(context.ModDirectory, "Mystery.png");
            var sprite = ModGambitApi.LoadSprite(spritePath);
            mySprite = sprite;

            // GambitBuilder is provided by sample_mods/GambitApi. It clones a vanilla
            // gambit prefab, fills in metadata, and attaches our BaseGambit subclass
            // to handle runtime behaviour.
            // This ID is also what the console sees for commands like
            // `give gambit mystery`, so keep it short and readable.
            var def = GambitBuilder.Create("mystery")
                .WithName("Mystery's Gambit")
                .WithDescription(defaultDescription)
                .WithRarity(Rarity.RARE)
                .WithFocus(Gambit_Focus.UTILITY)
                .WithPrice(6)
                .WithVisual(sprite)
                .WithVisualScale(1f)
                // This tells GambitApi to attach MysterysGambit to the in-run
                // gambit object. Without this, the card would exist but do nothing.
                .WithBaseGambit<GambitMystery>()
                // AutoUnlock means the gambit can appear immediately without adding
                // a separate unlock achievement.
                .AutoUnlock(true)
                .Register();
                
            context.LogLine($"[MysterysGambit] registered '{def.Id}'.");
        } 
        // The vanilla game stores its gambit titles and descriptions in the resources.assets file.
        // This method overwrites that file at the index "mystery_description"
        // Credit to Bentrd for laying the groundwork in GambitRegistry
        public static void UpdateMysteryDescription(string s = null)
        {
            var locManager = SingletonMonoBehaviour<LocalizationManager>.Instance;
            if (locManager == null)
                return;

            var traduction = locManager.GetTraduction();
            if (traduction == null)
                return;

            var gambitNode = traduction["gambit"];
            if (gambitNode == null)
                return;

            // Update the gambit's description with the passed string,
            // or with the default description if no argument was passed.
            gambitNode[$"mystery_description"] = s is not null? s : defaultDescription;
        }  
    }
}
