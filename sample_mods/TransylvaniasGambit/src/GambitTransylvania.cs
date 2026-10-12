using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;

namespace Gambonanza.TransylvaniasGambit
{
    /// <summary>
    /// Transylvania's Gambit: Selling this gambit earns 3 random Halloween-themed gambits.
	///
    /// When this gambit is destroyed, it selects three random gambit rarities, selects a random
    /// gambit ID from a pre-defined list for each rarity, and places them in empty gambit places.
	/// </summary>
    public sealed class GambitTransylvania : BaseGambit
    {
        // Define a list of Halloween-themed gambit IDs grouped by rarity
        public static string[][] gambitIDs =
        {
            // Common
            new string[]{"clown", "dungeon", "plague-mask", "shadow", "depthcreature", "alchemist",
                "doot", "frankenstein", "headless-horseman"},
            // Rare
            new string[]{"cauldron", "candle", "exorcist", "witch", "demon", "soul", "scream",
                "necromancer", "ouija", "soulLantern", "sacrifice", "possession", "skeleton",
				"vampire", "ParanormalActivity"},
            // Epic
            new string[]{"stallion", "warlock", "lich", "ghost", "back-stabber", "spider", "zombie"},
            // Legendary
            new string[]{"apocalypse", "joker", "transylvania"}
        };
        private void Start()
        {
        }

        private void OnDestroy()
        {
            // We don't want to earn gambits if we're leaving the game.
            State gameState = GameManager.Instance.CurrentState;
            if (gameState == State.PAUSE || gameState == State.MENU)
                return;
            // BOING!
            VisualEffect();
            // Find the gambit place this gambit is standing in and mark it as empty
            foreach(GambitPlaceBehaviour g in GambitManager.Instance.GambitPlaces)
            {
                if(g.CurrentGambit is not null && g.CurrentGambit.Info.ID.Equals("transylvania"))
                {
                    g.CurrentGambit = null;
                }
            }
            // To improve the diversity of earned gambits, I'm going to ensure that each
            // gambit is a different rarity. Here, we're defining one of the four rarities
            // not to use when looping through the gambitIDs array. 
            int skipRarity = Random.Range(0, 4);
            // for every "gambit rarity"...
            for (int i = 0; i < 4; i++)
            {
                // If there's no room for more gambits, stop the trigger early
                if (GambitManager.Instance.IsFull()) return;
                // If this is the rarity to skip, move to the next increment
                if (i == skipRarity) continue;
                // Find a random SO_Gambit of this rarity
                // NOTE: Ideally this loop should only run once, but I'm making this a
                // loop to check for errors.
                SO_Gambit soGambit = null;
                while (soGambit is null)
                {
                    string[] rarityIDs = gambitIDs[i];
                    string pickID = rarityIDs[Random.Range(0, rarityIDs.Length)];
                    soGambit = GambitLibrary.Instance.GetGambitPerId(pickID);
                }

                // Instantiate this gambit and move it to its respective gambit place (Pulled from AFK's gambit)
				GambitPlaceBehaviour gambitPlace = SingletonMonoBehaviour<GambitManager>.Instance.GetGambitPlace();
                GambitBehaviour newGambit = Object.Instantiate<GambitBehaviour>(SingletonMonoBehaviour<GambitLibrary>.Instance.Gambits[SingletonMonoBehaviour<GambitLibrary>.Instance.GambitsInfo.IndexOf(soGambit)], base.transform.position, Quaternion.identity, gambitPlace.GambitParent);
                gambitPlace.CurrentGambit = newGambit;
                newGambit.transform.localScale = Vector3.zero;
                TweenSettingsExtensions.SetEase<TweenerCore<Vector3, Vector3, VectorOptions>>(ShortcutExtensions.DOScale(newGambit.transform, Vector3.one, 0.3f), (Ease)27);
                TweenSettingsExtensions.SetDelay<Tweener>(newGambit.transform.DOFollow(gambitPlace.GambitParent, 0.2f), 0.3f);
                if (GambitManager.Instance.OnGetGambit != null)
                {
                    GambitManager.Instance.OnGetGambit.Invoke();
                }
            }
        }

        public override void Trigger()
        {
            // BOING!
            VisualEffect();
        }
    }
}
