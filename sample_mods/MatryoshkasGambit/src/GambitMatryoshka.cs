using System;
using System.Collections;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.MatryoshkasGambit
{
    /// <summary>
    /// Matryoshka's Gambit: If any of your pieces is captured, earn a piece of the next highest value.
    /// 
    /// This gambit replicates much of the logic of Wrecking Ball's Gambit, but making modifications
    /// to the checked and gifted piece types.
    /// </summary>
    public sealed class GambitMatryoshka : BaseGambit
    {
        private void Start()
        {
            EnemyManager.Instance.OnCapture += Behave;
        }

        private void OnDestroy()
        {
            EnemyManager.Instance.OnCapture -= Behave;
        }

        // This method passes 
        private void Behave(BasePieceBehaviour attacker, BasePieceBehaviour victim, TileBehaviour tile)
        {
            //We only want to trigger if there's room in the stock
            if (SingletonMonoBehaviour<StockManager>.Instance.RoomAvailable())
            {
                // Determine which piece type to earn
                PieceType type = new();
                switch (victim.GetPieceType())
                {
                    case PieceType.QUEEN:
                        type = PieceType.KNIGHT;
                        break;
                    case PieceType.KNIGHT:
                        type = PieceType.ROOK;
                        break;
                    case PieceType.ROOK:
                        type = PieceType.BISHOP;
                        break;
                    case PieceType.BISHOP:
                        type = PieceType.KING;
                        break;
                    case PieceType.KING:
                        type = PieceType.PAWN;
                        break;
                    // For pawns and other edge cases, stop the trigger early
                    default:
                        return;
                }
                base.StartCoroutine(this.CO_Effect(attacker, type, 0.31f));
            }
        }

        private IEnumerator CO_Effect(BasePieceBehaviour piece, PieceType type, float delay)
        {
            // Wait until after the piece capture animation
            yield return new WaitForSeconds(delay);
            // Add the piece to the stock
            SingletonMonoBehaviour<StockManager>.Instance.AddPiece(type, piece.transform.position, false, false, false, false);
            Trigger();
            yield break;
        }

        public override void Trigger()
        {
            // BOING!
            VisualEffect();
        }
    }
}
