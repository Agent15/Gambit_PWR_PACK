using System;
using System.Collections;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.VampiresGambit
{
    /// <summary>
    /// Vampire's Gambit: Immediately turns golden pieces default and earns $2 for each of them
    ///
    /// This gambit waits until the player's turn, checks for any gold pieces on screen, and
    /// mimics the effect of the end-of-game gold piece reset for each of them.
    /// </summary>
    public sealed class GambitVampire : BaseGambit
    {
        private void Start()
        {
            // On the player's turn, remove the gold property from all golden pieces
            TurnManager.Instance.OnPlayerTurn += SUCC;
            // For any edge cases, check for gold pieces when they turn gold as well
            PieceManager.Instance.OnGoldenizePiece += CO_SUCC;
        }

        private void OnDestroy()
        {
            // Unassign sction calls
            TurnManager.Instance.OnPlayerTurn -= SUCC;
            PieceManager.Instance.OnGoldenizePiece -= CO_SUCC;
        }

        // This method is an intermediary between the argument requirements of
        // OnGoldenizePiece and SuccWithDelay
        private void CO_SUCC(BasePieceBehaviour x)
        {
            base.StartCoroutine(SuccWithDelay(0.5f));
        }

        // This method will trigger the SUCC() method, but only after the specified delay
        private IEnumerator SuccWithDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            // Let the game handle any gold pieces the player makes
            // on their winning move.
            if (GameManager.Instance.CurrentState != State.WIN)
            {
                // Otherwise, sweep for gold pieces
                SUCC();
            }
        }

        private void SUCC()
        {
            // Collect a list of every piece on screen
            // NOTE: I'm intentionally not filtering for only white pieces. I want this gambit's
            // effect to include gold enemy pieces if enyone mods in that feature.
            BasePieceBehaviour[] allPieces = FindObjectsByType<BasePieceBehaviour>();
            bool succFlag = false;
            // For every piece...
            foreach (BasePieceBehaviour piece in allPieces)
            {
                // If this piece is gold
                if (piece.Modifier.IsGolden)
                {
                    // Remove this piece's gold property
                    SingletonMonoBehaviour<MoneyAnimationManager>.Instance.SpawnMoney(this.transform, 2);
                    SingletonMonoBehaviour<ChessDataManager>.Instance.IncreaseCoin(2);
                    piece.VisualEffect.GoldEffect();
                    piece.Modifier.ResetGold();
                    base.StartCoroutine(this.CO_RemoveGold(piece, 0.5f));
                    // Signal for a VisualEffect later
                    succFlag = true;
                }
            }
            if (succFlag)
            {
                // At least one piece was succ'd. BOING!
                VisualEffect();
            }
        }

        // The base game adds a delay to the ResetGold() and RemoveGold() executions.
        // Attemping to do these without a delay causes the piece to fade to its default
        // color then immediately blip back to its gold color, so there must be a
        // good reason for it.
        private IEnumerator CO_RemoveGold(BasePieceBehaviour piece, float delay)
        {
            yield return new WaitForSeconds(delay);
            piece.VisualEffect.RemoveGold();
            yield break;
        }

        public override void Trigger()
        {
            // In case of an external trigger, earn $2
            SingletonMonoBehaviour<MoneyAnimationManager>.Instance.SpawnMoney(this.transform, 2);
            SingletonMonoBehaviour<ChessDataManager>.Instance.IncreaseCoin(2);
            // BOING!
            VisualEffect();
        }

        // DEBUG
        public static void UpdateDescription(string s)
        {
            var locManager = SingletonMonoBehaviour<LocalizationManager>.Instance;
            if (locManager == null)
            {
                Debug.LogWarning("[GambitApi] LocalizationManager not found, tooltip text will be empty.");
                return;
            }

            // Force load if not cached
            var traduction = locManager.GetTraduction();
            if (traduction == null)
            {
                Debug.LogWarning("[GambitApi] GetTraduction() returned null.");
                return;
            }

            var gambitNode = traduction["gambit"];
            if (gambitNode == null)
            {
                Debug.LogWarning("[GambitApi] traduction['gambit'] node not found.");
                return;
            }


            gambitNode[$"vampire_description"] = s;
        }
    }
}
