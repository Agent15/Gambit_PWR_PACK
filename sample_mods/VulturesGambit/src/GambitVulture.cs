using System;
using System.Collections;
using System.Collections.Generic;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.VulturesGambit
{
    /// <summary>
    /// Vulture's Gambit: Capturing a trapped piece traps every piece that was defending it
	///
	/// </summary>
    public sealed class GambitVulture : BaseGambit
    {
        private void Start()
        {
            SelectionManager.Instance.OnCapture += Behave;
		}

        private void OnDestroy()
        {
            SelectionManager.Instance.OnCapture -= Behave;
        }

        private void Behave(BasePieceBehaviour x, BasePieceBehaviour victim, TileBehaviour captureTile)
        {
            // Only run the Coroutine if the captured piece is trapped
            if(victim.Modifier.IsTrapped)
            {
                base.StartCoroutine(CO_Behave(x, victim, captureTile));
            }
        }

        private IEnumerator CO_Behave(BasePieceBehaviour x, BasePieceBehaviour victim, TileBehaviour captureTile)
        {
            // Give a slight delay to let the board update first
            yield return new WaitForSeconds(0.2f);
            // Declare a bool variable to use for visual effects
            bool vultureFlag = false;
            // Find efery piece defending the captured tile
            // For each enemy piece...
            foreach(BasePieceBehaviour piece in EnemyManager.Instance.EnemyPieces)
            {
                // For every tile that piece can move to...
                foreach(TileBehaviour tile in piece.GetTilesAvailable())
                {
                    // If the capturing piece is on this tile
                    if(tile == captureTile)
                    {
                        vultureFlag = true;
                        piece.Modifier.TrapInstant(1);
                        if(PieceManager.Instance.OnTrap is not null)
                            PieceManager.Instance.OnTrap.Invoke(piece);
                        //Move on to the next piece
                        break;
                    }
                }
            }
            if(vultureFlag)
            {
                // At least one piece was trapped. BOING!
                VisualEffect();
            }
        }
        public override void Trigger()
        {
            // BOING!
            VisualEffect();
        }
    }
}
