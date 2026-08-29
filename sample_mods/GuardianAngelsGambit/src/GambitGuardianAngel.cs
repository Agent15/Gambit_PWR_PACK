using System;
using System.Collections;
using System.Collections.Generic;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.GuardianAngelsGambit
{
    /// <summary>
    /// Guardian Angel's Gambit: When any of your pieces is protected, bless it when the effect expires
    ///
    /// This gambit has three main functions
    /// 1. When any piece is protected, add it to a queue of pieces to bless later
    /// 2. On the player's turn, look through the list of protected pieces and bless any piece that
    ///     isn't protected anymore
    /// 3. Cleanup the protectedPieces list when entering the shop
    /// </summary>
    public sealed class GambitGuardianAngel : BaseGambit
    {
        private List<BasePieceBehaviour> protectedPieces = new();
        private void Start()
        {
            // When a piece is protected, trigger the Enqueue() method
            PieceManager.Instance.OnProtectPiece += Enqueue;
            // On the player's turn, trigger the Behave() method with a delay
            TurnManager.Instance.OnPlayerTurn += CO_Behave;
            // On certain state changes, trigger the Cleanup() method
            GameManager.Instance.onStateChanged += Cleanup;
        }

        private void OnDestroy()
        {
            // Unassign action calls
            PieceManager.Instance.OnProtectPiece -= Enqueue;
            TurnManager.Instance.OnPlayerTurn -= CO_Behave;
            GameManager.Instance.onStateChanged -= Cleanup;
        }

        private void Enqueue(BasePieceBehaviour piece)
        {
            // Add the protected piece to the list of pieces to bless later
            if (!protectedPieces.Contains(piece))
            {
                protectedPieces.Add(piece);
            }
        }

        // This method is an intermediary between the argument requirements of the
        // OnPlayerTurn action and the Behave() method.
        private void CO_Behave()
        {
            // Start the Behave() method with a half-second delay
            base.StartCoroutine(Behave(0.5f));
        }

        private IEnumerator Behave(float delay)
        {
            yield return new WaitForSeconds(delay);
            // If the protectedPieces list is empty, end this method early
            if (protectedPieces.Count == 0) yield break;
            // Create a copy of the protectedPieces list to remove pieces from
            // (Removing from the same list you're itterating through gets real messy)
            List<BasePieceBehaviour> queueClone = new List<BasePieceBehaviour>(protectedPieces);
            // For every piece in the copy of the protectedPieces list...
            foreach (BasePieceBehaviour piece in queueClone)
            {
                // If this piece still exists and is no longer protected...
                if (piece is not null && !piece.Modifier.IsProtected)
                {
                    // Bless this piece and send the the OnBlessPiece action call
                    piece.Modifier.Benediction();
                    piece.CurrentTile.TileVisual.BenedictionEffect();
                    if (PieceManager.Instance.OnBlessPiece is not null)
                    {
                        PieceManager.Instance.OnBlessPiece.Invoke(piece);
                    }
                    // Remove this piece from the true protectedPieces list if it still exists
                    if (protectedPieces.Contains(piece))
                    {
                        protectedPieces.Remove(piece);
                    }
                    // BOING!
                    VisualEffect();
                }
            }
        }
        public override void Trigger()
        {
            // BOING!
            VisualEffect();
        }

        private void Cleanup(State state)
        {
            // In case there are still pieces in the protectedPieces list,
            // clear the list when we enter the shop
            if (state == State.SHOP)
            {
                protectedPieces.Clear();
            }
        }
    }
}
