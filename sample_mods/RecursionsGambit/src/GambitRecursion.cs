using System;
using System.Collections;
using System.Collections.Generic;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;
using DG.Tweening;
using Gambonanza.PointAMngr;

namespace Gambonanza.RecursionsGambit
{
    /// <summary>
    /// Recursion's Gambit: Moving the same piece, the same distance,
    /// in the same direction as your last move skips the enemy turn
    ///
    /// This gambit combines the turn-skipping behavior of Mime's Gambit
    /// With some custom logic to determine the direction and distance of piece moves
	///
	/// WIP: It was never cruble tiles! It's a rounding error due to a change in stages!
    /// </summary>
    public sealed class GambitRecursion : BaseGambit
    {
        // Declare a set of variables to remember the last player move
        private (int x, int y) lastDelta = (0, 0);
        private BasePieceBehaviour lastPiece = null;

        private void Start()
        {
            //Assign this classes Behave() and Cleanup() methods to the game's actions
            SelectionManager.Instance.OnMove += CO_Behave;
            // In case you got this gambit mid-game, Populate PointAManager's pieceTracker
            PointAManager.Instance.InstantFill();
        }

        private void OnDestroy()
        {
            //Unssign this classes Behave() and Cleanup() methods from the game's actions
            SelectionManager.Instance.OnMove -= CO_Behave;
        }

        // This method is an intermediary between the argument requirements of OnMove and Behave
        private void CO_Behave(BasePieceBehaviour piece, TileBehaviour tile)
        {
            base.StartCoroutine(Behave(piece, tile, 0.1f));
        }

        //Trigger the gambit effect if this move is equal to last move, update the last move otherwise
        private IEnumerator Behave(BasePieceBehaviour movedPiece, TileBehaviour tile, float delay)
        {
            // Wait for PointAManager to update its attributes first
            yield return new WaitForSeconds(delay);
            
                // Null safety for PlayerPointA
                if (PointAManager.Instance.PlayerPointA is not null)
                {
                    (int x, int y) delta = PointAManager.GetDelta(PointAManager.Instance.PlayerPointA, tile);

                    if (movedPiece == lastPiece && delta.x == lastDelta.x && delta.y == lastDelta.y)
                    {
                        //This move was the same as the last move
                        Trigger();
                    }
                    else
                    {
                        //This move was different. Update the last move
                        lastPiece = movedPiece;
                        lastDelta.x = delta.x;
                        lastDelta.y = delta.y;
                    }
                }
            
        }

        public override void Trigger()
        {
            //Skip the enemy turn
            SingletonMonoBehaviour<EnemyManager>.Instance.SkipTurn();
            //BOING!
            this.VisualEffect();
        }
    }
}
