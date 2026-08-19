using System;
using System.Collections;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;
using DG.Tweening;

namespace Gambonanza.SchrodingersGambit
{
    /// <summary>
    /// Schrodinger's Gambit: If any of your phantom pieces is threatened, it becomes default.
    /// 
    /// This gambit combines the automatic threatened piece scanning behavior of Crybaby's gambit
    /// with the ResetPhantom call of Necromancer's gambit.
    /// </summary>
    public sealed class GambitSchrodinger : BaseGambit
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
            //Give TurnManager and EnemyManager some time to update first
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
                    foreach (BasePieceBehaviour target in piece.ThreatenBehaviour.TreathenPieces)
                    {
                        // If this threatened piece is phantom...
                        if (target.Modifier.IsPhantom)
                        {
                            // Remove its phantom property
                            target.Modifier.ResetPhantom();
                            // BOING!
                            VisualEffect();
                        }

                    }
                }
            }
        }
    }
}
