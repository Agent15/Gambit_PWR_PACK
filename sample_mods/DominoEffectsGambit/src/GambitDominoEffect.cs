using System;
using System.Collections;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;
using DG.Tweening;

namespace Gambonanza.DominoEffectsGambit
{
    /// <summary>
    /// Domino Effect's Gambit: Trapping an enemy piece traps every enemy piece adjacent to it.
    ///
    /// This gambit listens to every trap-related action call and responds with the adjacent
    /// trapping behavior of Makibishi's gambit, returning the same call that it listens to
    /// until there isn't a non-trapped adjacent piece.
    ///
    /// This gambit was a sugestion by Minty <3
    /// </summary>
    public sealed class GambitDominoEffect : BaseGambit
    {
        private void Start()
        {
            TileManager.Instance.OnHunterTileUsed += TileTrigger;
            PieceManager.Instance.OnTrap += PieceTrigger;
        }

        private void OnDestroy()
        {
            TileManager.Instance.OnHunterTileUsed -= TileTrigger;
            PieceManager.Instance.OnTrap -= PieceTrigger;
        }

        //This is an intermediary between the argument requirements of the OnHunterTileUsed and DominoTrap methods
        private void TileTrigger(BasePieceBehaviour piece, TileBehaviour x)
        {
            base.StartCoroutine(DominoTrap(piece));
        }

        //This is an intermediary between the argument requirements of the OnHunterTileUsed and DominoTrap methods
        private void PieceTrigger(BasePieceBehaviour piece)
        {
            base.StartCoroutine(DominoTrap(piece));
        }

        private IEnumerator DominoTrap(BasePieceBehaviour piece)
		{
			yield return new WaitForSeconds(0.2f);
            //If the trapped piece has to adjacent pieces, we don't want to trigger a visual effect
            bool boing = false;
            // For every tile adjacent to the trapped piece...
			foreach (TileBehaviour tile in piece.GetNeighbourTiles())
			{
                // If this tile has a non-trapped black piece on it
				if (tile.Piece != null && tile.Piece.PieceColor == PieceColor.BLACK && !tile.Piece.Modifier.IsTrapped)
				{
                    boing = true;
                    // Trap the piece and pass the action call again
					tile.Piece.Modifier.TrapInstant(1);
					PieceManager.Instance.OnTrap.Invoke(tile.Piece);
				}
			}
            if(boing)
            {
                // BOING!
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
