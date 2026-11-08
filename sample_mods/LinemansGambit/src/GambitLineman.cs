using System.Collections;
using System.Collections.Generic;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.LinemansGambit
{
    /// <summary>
    /// Lineman's Gambit: Protect all your pawns with an enemy pawn directly in front of them.
	///
    /// This gambit sweeps the entire board at the start of the game, and after every turn, and...
    /// - Protects every pawn with an enemy pawn directly in front of it
    /// - Removes the protection without an enemy pawn in front of it if it was protecting it already.
	/// </summary>
    public sealed class GambitLineman : BaseGambit
    {
        private List<BasePieceBehaviour> linemen = new();
        private void Start()
        {
            // Check the board at the start of the game and after every turn
            GameManager.Instance.onStateChanged += LinemanSweep;
            SelectionManager.Instance.OnMove += LinemanSweep;
            EnemyManager.Instance.OnMove += LinemanSweep;
            WaitManager.Instance.OnWait += LinemanSweep;
            // In case you earned this gambit mid-game, sweep the board immediately
            if (GameManager.Instance.CurrentState == State.INGAME)
                base.StartCoroutine(CO_Sweep(0.2f));
        }

        private void OnDestroy()
        {
            // Remove the protection from every piece this gambit is protecting
            base.StartCoroutine(LinemanReset(0f));
            // Unassign action calls
            GameManager.Instance.onStateChanged -= LinemanSweep;
            SelectionManager.Instance.OnMove -= LinemanSweep;
            EnemyManager.Instance.OnMove -= LinemanSweep;
            WaitManager.Instance.OnWait -= LinemanSweep;
        }

        // These methods is an intermediary between the argument requirements of 
        // CO_Sweep and the several action calls that trigger it.
        private void LinemanSweep(BasePieceBehaviour x, TileBehaviour y)
        {
            base.StartCoroutine(CO_Sweep(0.2f));
        }
        private void LinemanSweep()
        {
            base.StartCoroutine(CO_Sweep(0.2f));
        }
        private void LinemanSweep(State state)
        {
            if (state == State.INGAME)
                base.StartCoroutine(CO_Sweep(0.6f));
            if (state == State.WIN)
            {
                base.StartCoroutine(LinemanReset(0.6f));
            }
        }

        private IEnumerator CO_Sweep(float delay)
        {
            yield return new WaitForSeconds(delay);
            // Define a bool variable to use for visual effects
            bool linemanFlag = false;
            // Pull in the BoardManager's board for simplicity
            TileBehaviour[,] board = BoardManager.Instance.Board;
            // For every "row on the board"...
            for (int i = 0; i < board.GetLength(0); i++)
            {
                // For every "tile" in that "row"...
                for (int j = 0; j < board.GetLength(1); j++)
                {
                    // Define this tile
                    TileBehaviour tile = board[i, j];
                    // If this tile has a white pawn on it...
                    if (tile.Piece != null && tile.Piece.GetPieceType() == PieceType.PAWN && tile.Piece.PieceColor == PieceColor.WHITE)
                    {
                        // Define the pawn and the tile directly in front of it
                        BasePieceBehaviour pawn = tile.Piece;
                        TileBehaviour tileAbove = null;
                        try { tileAbove = board[i - 1, j]; } catch { }
                        // If this pawn NOT in the linemen list and...
                        if (!linemen.Contains(pawn) &&
                        // There's an enemy pawn directly in front of it...
                        (
                            tileAbove is not null && tileAbove.Piece is not null
                            && tileAbove.Piece.GetPieceType() == PieceType.PAWN
                            && tileAbove.Piece.PieceColor == PieceColor.BLACK)
                        )
                        {
                            // Protect the pawn and add it to the linemen list
                            LinemanProtect(pawn);
                            linemen.Add(pawn);
                            linemanFlag = true;
                        }
                        // If this pawn is in the linemen list and...
                        else if (linemen.Contains(pawn) &&
                        // There is NOT an enemy pawn directly in front of it...
                        !(
                            tileAbove is not null && tileAbove.Piece is not null
                            && tileAbove.Piece.GetPieceType() == PieceType.PAWN
                            && tileAbove.Piece.PieceColor == PieceColor.BLACK)
                        )
                        {
                            // Remove its protection and remove it from the linemen list
                            LinemanUnprotect(pawn);
                            linemen.Remove(pawn);
                        }
                    }
                }
            }
            if (linemanFlag)
            {
                // At least one new pawn was protected. BOING!
                VisualEffect();
            }
        }

        private void LinemanProtect(BasePieceBehaviour pawn)
        {
            // Mark this piece as protected
            pawn.Modifier.IsProtected = true;
            // Display the protection aura over the piece
            pawn.VisualEffect.ShowShield();
            // Let other parts of the game know a piece was protected
            if (PieceManager.Instance.OnProtectPiece is not null)
                PieceManager.Instance.OnProtectPiece.Invoke(pawn);
        }

        private void LinemanUnprotect(BasePieceBehaviour pawn)
        {
            // Unmark this piece as protected
            pawn.Modifier.IsProtected = false;
            // Remove the protective aura
            pawn.VisualEffect.HideShield();
        }
        private IEnumerator LinemanReset(float delay)
        {
            yield return new WaitForSeconds(delay);
            foreach (BasePieceBehaviour pawn in linemen)
            {
                LinemanUnprotect(pawn);
            }
            linemen.Clear();
        }
        public override void Trigger()
        {
            // BOING!
            VisualEffect();
            base.StartCoroutine(CO_Sweep(0.2f));
        }
    }
}
