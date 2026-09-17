using System.Collections;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;
using Gambonanza.PointAMngr;

namespace Gambonanza.BackStabbersGambit
{
    /// <summary>
    /// Back-Stabber's Gambit: Capturing with a backward move skips the enemy's turn.
	///
    /// This gambit listens for the OnCapture action call, checks the displacement through
    /// PointAManager, and skips the enemy's turn if the vertical component is less than 0
	/// </summary>
    public sealed class GambitBackStabber : BaseGambit
    {
        private void Start()
        {
            // When the player captures, execute the Behave() method
            SelectionManager.Instance.OnCapture += Behave;
            // In case this gambit was earned mid-game, update PointAManager
            PointAManager.Instance.InstantFill();
		}

        private void OnDestroy()
        {
            // Unassign action calls
            SelectionManager.Instance.OnCapture -= Behave;
        }

        // This method is an intermediary between the argument requirements of OnCapture and CO_Behave
        private void Behave(BasePieceBehaviour attacker, BasePieceBehaviour victim, TileBehaviour tile)
        {
            base.StartCoroutine(CO_Behave(attacker));
        }

        private IEnumerator CO_Behave(BasePieceBehaviour piece)
        {
            // Wait for PointAManager to update first
            yield return new WaitForSeconds(0.1f);
            // Get the displacement of the player's movement
            (int x, int y) delta = PointAManager.GetDelta(PointAManager.Instance.PlayerPointA, piece.CurrentTile);
            // Execute the trigger method if this move was downward (south)
            if(delta.y < 0)
                Trigger();
        }

        public override void Trigger()
        {
            //Skip the enemy turn
            SingletonMonoBehaviour<EnemyManager>.Instance.SkipTurn();
            // BOING!
            VisualEffect();
        }
    }
}
