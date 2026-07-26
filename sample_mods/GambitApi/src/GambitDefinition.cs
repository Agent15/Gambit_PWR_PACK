using System;
using Blukulele.CHE;
using UnityEngine;

namespace Gambonanza.GambitApi
{
    /// <summary>
    /// Holds all configuration data needed to create and register a custom gambit.
    /// </summary>
    public class GambitDefinition
    {
        public string Id;
        public string Name = "New Gambit";
        public string Description = "A custom gambit.";
        public Sprite Visual;
        public int PriceCost = 5;
        public Rarity Rarity = Rarity.COMMON;
        public Gambit_Focus[] Focus = new[] { Gambit_Focus.UTILITY };
        public Unlock_Infos UnlockInfo = Unlock_Infos.NONE;
        public int GambitToUnlockToHaveAHint;

        // UI explanation flags
        public bool ShowPromotion;
        public bool ShowBless;
        public bool ShowGolden;
        public bool ShowProtect;
        public bool ShowTrap;
        public bool ShowPhantom;
        public bool ShowWait;
        public bool ShowGoldenTile;
        public bool ShowBlessedTile;
        public bool ShowProtectedTile;
        public bool ShowTrapTile;
        public bool ShowPhantomTile;
        public bool ShowLanding;
        public bool ShowConsideredAs;

        /// <summary>
        /// ID of the vanilla gambit to clone the prefab from (e.g. "COWBOY").
        /// If null, the API will attempt to find any available gambit prefab.
        /// </summary>
        public string TemplateGambitId;

        /// <summary>
        /// The concrete BaseGambit type to attach to the prefab.
        /// Defaults to SimpleGambit when using OnTrigger.
        /// </summary>
        public Type BaseGambitType;

        /// <summary>
        /// Trigger action used by SimpleGambit.
        /// </summary>
        public Action<GambitBehaviour> TriggerAction;

        /// <summary>
        /// Whether to auto-unlock this gambit so it appears in the shop.
        /// </summary>
        public bool AutoUnlock = true;

        /// <summary>
        /// On-board scale multiplier for the in-game sprite. 1.0 matches the cloned vanilla
        /// template's world height exactly. Use a value below 1 to shrink (handy when your
        /// art is more tightly cropped than vanilla and ends up looking visually larger),
        /// or above 1 to grow. Only affects the in-world piece - collection art is untouched.
        /// </summary>
        public float VisualScale = 1f;
    }
}
