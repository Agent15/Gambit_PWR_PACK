using System.Collections;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.MoneyTreesGambit
{
    /// <summary>
    /// MoneyTree's Gambit: At the end of a game, earn $X (Increases by +$1 each game).
	///
    /// At the end of a game, this gambit executes a Trigger() function that earns the 
    /// current value in its dollarCount attribute and increases that same attribute by 1.
	/// </summary>
    public sealed class GambitMoneyTree : BaseGambit
    {
        int dollarCount = 1;
        private void Start()
        {
            // On certain state changes, execute the Behave() method
            GameManager.Instance.onStateChanged += Behave;
		}

        private void OnDestroy()
        {
            // Unassign action calls
            GameManager.Instance.onStateChanged -= Behave;
        }

        private void Behave(State state)
        {
            // Don't do anything if resuming from a pause
			if (GameManager.Instance.PreviousState == State.PAUSE
                || GameManager.Instance.PreviousState == State.RUN_INFO)
			{
				return;
			}
            // Trigger the gambit after winning a game
			if (state == State.WIN)
			{
				Trigger();
            }
        }

        public override void Trigger()
        {
            // Update the game's dollar count
            SingletonMonoBehaviour<ChessDataManager>.Instance.IncreaseCoin(dollarCount);
            // Generate floating money symbols
			SingletonMonoBehaviour<MoneyAnimationManager>.Instance.SpawnMoney(base.transform, dollarCount);
            // BOING!
			VisualEffect();
            // Increase the gambit's dollar count
            base.StartCoroutine(Increment(0.5f));
        }

        // Increase the gambit's dollar count (after a short delay, to separate the cue from the visual effect)
        private IEnumerator Increment(float delay)
        {
            yield return new WaitForSeconds(delay);
            // Increase the dollar count by 1
            dollarCount++;
            // Update the gambit's description
            UpdateDescription(dollarCount);

            // Play a small cue that the target type changed
			this.m_FeedbackIncrementor.Spawn("+1");
			this.m_FeedbackIncrementor.IncrementSound(0f);
        }

	    public void UpdateDescription(int n)
        {
            string s = $"At the end of a game, earn <color=*>${n}</color> (Increases by <color=*>+$1</color> each game).";
            var locManager = SingletonMonoBehaviour<LocalizationManager>.Instance;
            if (locManager == null)
                return;

            var traduction = locManager.GetTraduction();
            if (traduction == null)
                return;

            var gambitNode = traduction["gambit"];
            if (gambitNode == null)
                return;
            
            gambitNode["money-tree_description"] = s;
        }  
    }
}
