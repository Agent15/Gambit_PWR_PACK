using System;
using System.Collections;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.CrybabysGambit
{
    /// <summary>
    /// Crybaby's Gambit: When any of your pieces is threatened, it is also counted as a capture
    /// 
    /// This gambit combines the automatic threat response of Bait's gambit with the threatened-piece-scanning
    /// behavior of Impossible Choice's gambit. Instead of earning money, each piece will run a chance and
    /// invoke an OnCapture call.
    ///
    /// WIP: Playing a game with this gambit, then going to the main menu, then playing another game makes the
    /// player's pieces impossible to touch. I'm still looking for the cause of the problem.
    /// </summary>
    public sealed class GambitCrybaby : BaseGambit
    {
        private void Start()
        {
            // Assign this gambit's CO_Behave method to the OnPlayerTurn action
            TurnManager.Instance.OnPlayerTurn += CO_Behave;
        }

        private void OnDestroy()
        {
            // Unassign this gambit's CO_Behave method from the OnPlayerTurn action 
            TurnManager.Instance.OnPlayerTurn -= CO_Behave;
        }

        public override void Trigger()
		{
            // BOING!
            VisualEffect();
        }

        //This is an intermediary between the argument requirements of the OnPlayerTurn and Behave functions
        private void CO_Behave()
        {
            //Wait until right after your pieces are threatened
            base.StartCoroutine(this.Behave(SingletonMonoBehaviour<FlowManager>.Instance.ThreatenDelay + 0.01f));
        }

        private IEnumerator Behave(float delay)
        {
            yield return new WaitForSeconds(delay);
            //For every enemy piece...
			foreach (BasePieceBehaviour piece in SingletonMonoBehaviour<EnemyManager>.Instance.EnemyPieces)
			{
                // If this enemy piece is threatening something...
				if (piece.ThreatenBehaviour.TreathenPieces.Count > 0)
				{
                    // For every piece it's threatening...
                    // (Note: This does mean that there are some niche cases where a doubly-threatened piece can
                    // trigger this gambit twice, but if it's good enough for Blukulele, it's good enough for me)
					foreach (BasePieceBehaviour target in piece.ThreatenBehaviour.TreathenPieces)
					{
                        //This piece pretends to be captured by itself
                        EnemyManager.Instance.OnCapture.Invoke(target, target, target.CurrentTile);
                        //BOING!
					    VisualEffect();
                        
					}
				}
			}
        }
    }
}
