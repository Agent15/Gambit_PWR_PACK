using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.PitStopsGambit
{
    /// <summary>
    /// PitStop's Gambit: If a game ends in stalemate, earn $10 and return to the shop.
	///
	/// </summary>
    public sealed class GambitPitStop : BaseGambit
    {
        private void Start()
        {
            if (PitStopsGambitBuild.stalemateHappened)
            {
                PitStopsGambitBuild.stalemateHappened = false;
                base.StartCoroutine(CO_Shop(5f));
            }
            // On stalemete, flip the boolean variable
            TurnManager.Instance.OnPat += Stalemate;
        }

        private void OnDestroy()
        {
            // Unassign action calls
            TurnManager.Instance.OnPat -= Stalemate;
        }

        private void Stalemate()
        {
            PitStopsGambitBuild.stalemateHappened = true;
        }

        public override void Trigger()
        {
            base.StartCoroutine(CO_Shop(0f));
        }

        private IEnumerator CO_Shop(float delay)
        {
            yield return new WaitForSeconds(delay);
            //BOING!
            VisualEffect();
            // Update the dollar count
            SingletonMonoBehaviour<ChessDataManager>.Instance.IncreaseCoin(10);
            // Generate floating money symbols
            SingletonMonoBehaviour<MoneyAnimationManager>.Instance.SpawnMoney(base.transform, 10);
            //Make every enemy piece on the board disappear
            EnemyManager.Instance.GetAllPieces();
            foreach (BasePieceBehaviour piece in EnemyManager.Instance.EnemyPieces)
            {
                PhantomDisappear(piece);
            }
            //If we're in the preparation phase...
            if (GameManager.Instance.CurrentState == State.BOARD_PLACEMENT)
            {
                // Find any canvas with a "GO" button and click it
                CanvasPreparation PrepCanvas = FindAnyObjectByType<CanvasPreparation>();
                PrepCanvas.Ready();
                yield return new WaitForSeconds(1.5f);
            }
            // Find any canvas for mid-game stats and hide it
            CanvasInGame GameCanvas = FindAnyObjectByType<CanvasInGame>();
            GameCanvas.Hide();
            // Send out an action call to go to the shop
            GameManager.Instance.CurrentState = State.WIN;
            GameManager.Instance.Shop();
        }

        // This is a copy of PhantomPieceManager's behavior to make a phantom piece disappear from the board.
        // It can't be called from outside its own class, so I copy/pasted and refactored it into this gambit.
        private void PhantomDisappear(BasePieceBehaviour pieceBehaviour)
        {
            Instantiate<GameObject>(PhantomPieceManager.Instance.ParticlePhantomDisappear, pieceBehaviour.transform.position + Vector3.up * 0.5f, Quaternion.identity);
            pieceBehaviour.VisualEffect.PhantomDisappearEffect();
            Destroy(pieceBehaviour.gameObject, 1f);
        }
        // DEBUG
        public static void UpdateDescription(string s)
        {
            var locManager = SingletonMonoBehaviour<LocalizationManager>.Instance;
            if (locManager == null)
                return;

            var traduction = locManager.GetTraduction();
            if (traduction == null)
                return;

            var gambitNode = traduction["gambit"];
            if (gambitNode == null)
                return;

            gambitNode["pit-stop_description"] = s;
        }
    }
}
