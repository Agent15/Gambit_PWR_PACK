using System;
using System.Collections;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.InternsGambit
{
    /// <summary>
    /// Intern's Gambit: The enemy pawns instead promote to random pieces.
	///
    /// This gambit checks every enemy move. If that move is "a pawn moving to the end of theboard",
    /// it waits until the promotion animation plays, then hot-swaps the newly promoted queen
    /// with a new enemy piece of a random piece type.
	/// </summary>
    public sealed class GambitIntern : BaseGambit
    {
        private void Start()
        {
            EnemyManager.Instance.OnMove += Behave;
        }

        private void OnDestroy()
        {
            EnemyManager.Instance.OnMove -= Behave;
        }

        private void Behave(BasePieceBehaviour piece, TileBehaviour tile)
        {
            // The base game doesn't have an action call for enemy promotions, so I'm checking every enemy move for
            // the same condition that triggers an enemy promotion instead.
            if (tile.IsEnd && tile.PromoteColor == PieceColor.BLACK && piece.PieceHierarchy == PieceHierarchy.PAWN)
            {
                base.StartCoroutine(CO_Demote(tile));
            }
        }

        private IEnumerator CO_Demote(TileBehaviour tile)
        {
            // Wait until just after the promotion animation
            yield return new WaitForSeconds(1.1f);
            // Select a random integer to convert to a PieceType later
            int selectedType = UnityEngine.Random.Range(0, 6);
            // If the random selected type is a Queen, just let the animation play out
            if (selectedType != (int)PieceType.QUEEN)
            {
                // The promoted pawn has already been destroyed, so we need to
                // execute Demote() on whatever piece is standing on that tile now.
                Demote(tile.Piece, (PieceType)selectedType);
            }
            // Whatever happens, BOING!
            VisualEffect();
        }

        // This method is an isolated sequence to transform a promoted piece to a different type
        // This logic is largely copy/pasted from Evangelist's gambit with several unnecesary parts removed
        public void Demote(BasePieceBehaviour piece, PieceType type)
        {
            // Create a new piece with the the Black piece color, but with the specified piece type
            BasePieceBehaviour newPiece = Instantiate<BasePieceBehaviour>(SingletonMonoBehaviour<Library>.Instance.GetPiece(type, PieceColor.BLACK), piece.transform.position, Quaternion.identity);
            // We DO want a flashy promotion-like animation
            newPiece.GetComponent<PieceApparitionEffect>().LaunchAnimationAtStart = true;
            // Register the new piece with PieceManager and copy any special modifiers (phantom, protected, etc.)
            SingletonMonoBehaviour<PieceManager>.Instance.CopyPieceCaractericstics(piece, newPiece, piece.CurrentTile);
            // Display the promotion icon on the promoted piece
            if (!PromotionManager.Instance.CEOGambitActivated)
            {
                newPiece.Modifier.ShowPromotionIcon();
            }
            // The CopyPieceCharacteristics() method doesn't check for the statis enemy modifier,
            // so I've copy/pasted this block from PromotionManager
            if (piece.EnemyAbilityModifier.IsClock)
            {
                newPiece.EnemyAbilityModifier.CurrentClockCounter = piece.EnemyAbilityModifier.CurrentClockCounter;
                newPiece.EnemyAbilityModifier.ClockBossPower(true);
            }
            // Destroy the original piece
            Destroy(piece.gameObject);
        }

        public override void Trigger()
        {
            // BOING!
            VisualEffect();
        }
    }
}
