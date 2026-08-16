using System;
using System.Collections;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;
using Gambonanza.PointAMngr;

namespace Gambonanza.SnipersGambit
{
    /// <summary>
    /// Sniper's Gambit: Moving the same piece, the same distance,
    /// in the same direction as your last move skips the enemy turn
    ///
    /// This gambit combines the turn-skipping behavior of Mime's Gambit
    /// With some custom logic to determine the distance of piece moves
    /// </summary>
    public sealed class GambitSniper : BaseGambit
    {
        private void Start()
        {
            // Assign this classes Behave() method to the game's actions
            SelectionManager.Instance.OnCapture += CO_Behave;
            // In case you got this gambit mid-game, Populate PointAManager's pieceTracker
            PointAManager.Instance.InstantFill();
        }

        private void OnDestroy()
        {
            // Unassign this classes Behave() method from the game's actions
            SelectionManager.Instance.OnCapture -= CO_Behave;
        }

        // This method is an intermediary between the argument requirements of OnMove and Behave
        private void CO_Behave(BasePieceBehaviour attacker, BasePieceBehaviour victim, TileBehaviour tile)
        {
            base.StartCoroutine(Behave(attacker, tile, 0.1f));
        }

        //Trigger the gambit effect if this move is equal to last move, update the last move otherwise
        private IEnumerator Behave(BasePieceBehaviour piece, TileBehaviour tile, float delay)
        {
            // Wait for PointAManager to update its attributes first
            yield return new WaitForSeconds(delay);
            // This logic should only execute for bishops
            if(piece.GetPieceType() == PieceType.BISHOP)
            {
                // Find the displacement of this bishop's move
                (int x, int y) delta = PointAManager.GetDelta(PointAManager.Instance.PlayerPointA, tile);

                // Check if either component is greater than or equal to 3
                //
                // - "bUt FrO_pWr, iF i MoVe TwO sPaCeS lEfT aNd TwO sPaCeS uP, i MoVeD fOuR sPaCeS"
                //
                // Actually, moving diagonal two spaces means you moved a distance of 2(√2) = 2.828 < 3
                // We use euclidean distance here. Manhattan distance is for BABIES!
                if(Math.Abs(delta.x) >= 3 || Math.Abs(delta.y) >= 3)
                {
                    Trigger();
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
