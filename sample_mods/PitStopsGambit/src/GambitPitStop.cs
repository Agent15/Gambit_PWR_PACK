using System.Collections;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.PitStopsGambit
{
    /// <summary>
    /// PitStop's Gambit: If a game ends in stalemate, earn $10 and return to the shop.
	///
    /// This gambit listens for a stalemate, then rewards $10 and uses several in-game
    /// action calls to force the game back to the shop without incrementing the game count.
	/// </summary>
    public sealed class GambitPitStop : BaseGambit
    {
        private void Start()
        {
            // If this gambit is respawning after a stalemate,
            // Execute the trigger after 5 seconds.
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
            // :3
            base.StartCoroutine(CO_Shop(0f));
        }

        private IEnumerator CO_Shop(float delay)
        {
            // Wait for the specified delay
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
                // Wait for the game's animations to play out
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
    }
}
