using System;
using System.Collections;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.GrandMasCookiesGambit
{
    /// <summary>
    /// GrandMa's Cookies' Gambit: At the start of a game, grants one random piece.
    ///
    /// This Gambit is a splice of two existing gambits in vanilla, combining
    /// the declaration and behavior of GrandMa's Gift's Gambit with the
    /// Trigger() effect of Chrysalis' gambit.
    /// </summary>
    public sealed class GambitGrandMasCookies : BaseGambit
    {
        //These are artifacts from GrandMa's Gift's Gambit.
        //I don't know what exactly what they do, but I don't dare remove them.
        private readonly string m_OccurenceName = "GRAND_MA_COOKIE";
        private readonly string m_OccurenceName_3 = "WEIGHTED_RANDOM_GAMBIT_2";
        private bool m_SaveOccurrence;
        private bool m_SaveOccurrence_3;

        private void Start()
        {
            // Register event callbacks
            GameManager.Instance.onStateChanged += Behave;
            TurnManager.Instance.OnPlayerTurn += SaveOccurrence;
            BuildBalanceManager.Instance.OnUseWeightedRandom += SaveOccurrence_3;
        }

        private void OnDestroy()
        {
            // Decrement occurrence counts if flags were set before destruction
            if (m_SaveOccurrence)
            {
                OccurrenceManager.Instance.GetAndDecrement(m_OccurenceName);
                OccurrenceManager.Instance.GetAndDecrement("WEIGHTED_RANDOM_GAMBIT_1");

                if (m_SaveOccurrence_3)
                {
                    OccurrenceManager.Instance.GetAndDecrement(m_OccurenceName_3);
                }
            }

            // Safely unsubscribe events (checking implicit bool conversion for UnityEngine.Object)
            if (GameManager.Instance)
            {
                GameManager.Instance.onStateChanged -= Behave;
            }

            if (TurnManager.Instance)
            {
                TurnManager.Instance.OnPlayerTurn -= SaveOccurrence;
            }

            if (BuildBalanceManager.Instance)
            {
                BuildBalanceManager.Instance.OnUseWeightedRandom -= SaveOccurrence_3;
            }
        }

        private void SaveOccurrence_3()
        {
            m_SaveOccurrence_3 = true;
        }

        private void SaveOccurrence()
        {
            m_SaveOccurrence = false;
            m_SaveOccurrence_3 = false;
        }

        //Executes on every state change and checks if it's currently the start of a game
        private void Behave(State state)
        {
            // Note: State values correspond to integer values in the State enum:
            // 15 = Enum state check 1
            // 19 = Enum state check 2
            // 1  = Target active state
            // 6  = Reset state / trigger condition state
            State previousState = GameManager.Instance.PreviousState;

            if ((int)previousState == 15 || (int)previousState == 19)
                return;

            if ((int)state == 1 && (int)previousState == 6)
            {
                Trigger();
            }

            if ((int)state == 6)
            {
                m_SaveOccurrence = false;
                m_SaveOccurrence_3 = false;
            }
        }

        public override void Trigger()
        {
            //Check if the stock is already full
            if (StockManager.Instance.GetPieceInStockCount() >= StockManager.Instance.GetMaxCount())
            {
                return;
            }
            // BOING!
            VisualEffect();

            // Determine piece type to add using randomized probability table
            PieceType pieceType = (PieceType)ChessDataManager.Instance.GetRandomOccurrence("SCAPEGOAT", 0, 6, -1);

            // Add the new piece to the stock 
            StockManager.Instance.AddPiece(
                pieceType,
                transform.position,
                false,
                false,
                false,
                false
            );
        }
    }
}
