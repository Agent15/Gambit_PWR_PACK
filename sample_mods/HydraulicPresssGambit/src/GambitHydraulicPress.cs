using System;
using System.Collections;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;
using Gambonanza.PointAMngr;

namespace Gambonanza.HydraulicPresssGambit
{
    /// <summary>
    /// Hydraulic Press's Gambit: Capturing with a rook on an adjacent tile skips the enemy's turn.
    ///
    /// This gambit is largely copied over from Sniper's gambit, only refactoring the Behave()
    /// method to check for rooks and adjacent tiles
    /// </summary>
    public sealed class GambitHydraulicPress : BaseGambit
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
            // This logic should only execute for "Rooks"
            if (piece.GetPieceType(false) == PieceType.ROOK)
            {
                // Calculate the offset of the player's move
                (int x, int y) delta = PointAManager.GetDelta(PointAManager.Instance.PlayerPointA, tile);
                // If this move was one tile orthoganally...
                // NOTE: TileBehaviour's GetNeighbourTiles() method only returns tiles with pieces on them
                // So I need to calculate the adjacent behavior manually
                if ((Math.Abs(delta.x) == 1 && Math.Abs(delta.y) == 0)
                    || (Math.Abs(delta.x) == 0 && Math.Abs(delta.y) == 1))
                {
                    //Trigger the turn skip
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
