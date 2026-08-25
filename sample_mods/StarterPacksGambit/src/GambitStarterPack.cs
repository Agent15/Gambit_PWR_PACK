using System;
using System.Collections;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;
using DG.Tweening;

namespace Gambonanza.StarterPacksGambit
{
    /// <summary>
    /// StarterPack's Gambit: Buying a gambit in the shop also earns a piece of its respective type.
    /// 
    /// This gambit combines the gambit-get tracking of Graal's Gambit with the piece earning
    /// behavior of GrandMa's Cookies Gambit.
    /// </summary>
    public sealed class GambitStarterPack : BaseGambit
    {
        // Define a small library to match gambit focuses to piece types
        public (Gambit_Focus focus, PieceType type)[] lookup =
        {
            (Gambit_Focus.PAWN, PieceType.PAWN),
            (Gambit_Focus.ROOK, PieceType.ROOK),
            (Gambit_Focus.KNIGHT, PieceType.KNIGHT),
            (Gambit_Focus.BISHOP, PieceType.BISHOP),
            (Gambit_Focus.QUEEN, PieceType.QUEEN),
            (Gambit_Focus.KING, PieceType.KING)
        };
        private void Start()
        {
            // When a gambit is bought, execute the Bahave() method
            GambitManager.Instance.OnBuyGambit += Behave;
        }

        private void OnDestroy()
        {
            // Unassign action calls
            GambitManager.Instance.OnBuyGambit -= Behave;
        }

        public void Behave(SO_Gambit gambit)
        {
            // For every synergy this gambit has
            foreach (Gambit_Focus synergy in gambit.Focus)
            {
                // If the stock has room and this synergy is in the lookup table
                if (StockManager.Instance.GetPieceInStockCount() < StockManager.Instance.GetMaxCount()
                    && Array.Exists(lookup, f => f.focus == synergy))
                {
                    // Lookup the corresponding piece type and earn a piece of that type
                    PieceType type = Array.Find(lookup, f => f.focus == synergy).type;
                    StockManager.Instance.AddPiece(type, transform.position);
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
    }
}
