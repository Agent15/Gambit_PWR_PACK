using System;
using System.Collections;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;
using DG.Tweening;

namespace Gambonanza.BountyHuntersGambit
{
    /// <summary>
    /// Bounty Hunter's Gambit: Capturing a [target piece] earns $5. (Changes after every trigger)
    /// 
    /// This gambit takes inspiration from the capture-checking logic of Mime's Gambit with
    /// the money-earning behavior of Spartan's Gambit.
    /// </summary>
    public sealed class GambitBountyHunter : BaseGambit
    {
        // Define the piece that grants a reward and a randomizer to select the next one
        private PieceType targetType = PieceType.NONE;
        private System.Random pick = new();
        private void Start()
        {
            // After every player capture, execute the Behave() method
            SelectionManager.Instance.OnCapture += Behave;
            // Decide the first target piece type
            base.StartCoroutine(Randomize(0.5f));
        }

        private void OnDestroy()
        {
            // Unassign action calls
            SelectionManager.Instance.OnCapture -= Behave;
        }

        // Checks for the type of the piece that was captured. Triggers and randomizes
        // its next target if the piece types match
        private void Behave(BasePieceBehaviour x, BasePieceBehaviour victim, TileBehaviour y)
        {
            if(victim.GetPieceType() == targetType)
            {
                Trigger();
            }
        }

        public override void Trigger()
        {
            // Update the dollar count
            SingletonMonoBehaviour<ChessDataManager>.Instance.IncreaseCoin(5);
            // Generate floating money symbols
			SingletonMonoBehaviour<MoneyAnimationManager>.Instance.SpawnMoney(base.transform, 5);
            // BOING!
			VisualEffect();
            // Pick the next tarteg type
            base.StartCoroutine(Randomize(0.5f));
        }

        // Determine the next target piece type (after a short delay, to separate the cue from the visual effect)
        private IEnumerator Randomize(float delay)
        {
            yield return new WaitForSeconds(delay);
            // Set an initial random value for the next piece type
            int nextTarget = pick.Next(6);
            // Keep randomizing until you get a different target type than before
            while(nextTarget == (int) targetType && nextTarget < 6)
            {
                nextTarget = pick.Next(6);
            }
            // Tranlsate this int into a PieceType value and assign it to targetType
            targetType = (PieceType)nextTarget;
            // Update the gambit's description
            BountyHuntersGambitBuild.UpdateDescription(targetType);

            // Play a small cue that the target type changed
			this.m_FeedbackIncrementor.Spawn("New\ntarget");
			this.m_FeedbackIncrementor.IncrementSound(0f);
        }
    }
}
