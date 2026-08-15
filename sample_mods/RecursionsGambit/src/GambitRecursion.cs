using System;
using System.Collections;
using System.Collections.Generic;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;
using DG.Tweening;
using Gambonanza.PointAMngr;

namespace Gambonanza.RecursionsGambit
{
    /// <summary>
    /// Recursion's Gambit: Moving the same piece, the same distance,
    /// in the same direction as your last move skips the enemy turn
    ///
    /// This gambit combines the turn-skipping behavior of Mime's Gambit
    /// With some custom logic to determine the direction and distance of piece moves
	///
	/// WIP: It was never cruble tiles! It's a rounding error due to a change in stages!
    /// </summary>
    public sealed class GambitRecursion : BaseGambit
    {
        // Declare a set of variables to remember the last player move
        private (int x, int y) lastDelta = (0, 0);
        private BasePieceBehaviour lastPiece = null;

        private void Start()
        {
            //Assign this classes Behave() and Cleanup() methods to the game's actions
            SelectionManager.Instance.OnMove += CO_Behave;
            GameManager.Instance.onStateChanged += Reset;
            // Create an instance of PointAManager if one hasn't already been made
            try
            {
                if (PointAManager.Instance == null)
                {
                    UpdateDescription("Still null");
                }
                else
                {
                    UpdateDescription(PointAManager.Instance.ToString());
                }
            }
            catch (Exception e)
            {
                UpdateDescription(e.ToString().Substring(startIndex: 0, length: 200));
            }
        }

        private void OnDestroy()
        {
            //Unssign this classes Behave() and Cleanup() methods from the game's actions
            SelectionManager.Instance.OnMove -= CO_Behave;
            GameManager.Instance.onStateChanged -= Reset;
        }

        // This method is an intermediary between the argument requirements of OnMove and Behave
        private void CO_Behave(BasePieceBehaviour piece, TileBehaviour tile)
        {
            base.StartCoroutine(Behave(piece, tile, 0.1f));
        }

        //Trigger the gambit effect if this move is equal to last move, update the last move otherwise
        private IEnumerator Behave(BasePieceBehaviour movedPiece, TileBehaviour tile, float delay)
        {
            // Wait for PointAManager to update its attributes first
            yield return new WaitForSeconds(delay);
            try
            {
                // Null safety for PlayerPointA
                if (PointAManager.Instance.PlayerPointA is not null)
                {
                    (int x, int y) delta = PointAManager.GetDelta(PointAManager.Instance.PlayerPointA, tile);

                    if (movedPiece == lastPiece && delta.x == lastDelta.x && delta.y == lastDelta.y)
                    {
                        //This move was the same as the last move
                        Trigger();
                    }
                    else
                    {
                        //This move was different. Update the last move
                        lastPiece = movedPiece;
                        lastDelta.x = delta.x;
                        lastDelta.y = delta.y;
                    }
                    // DEBUG: Gimme the info
                    Vector3 pointA = PointAManager.Instance.PlayerPointA.Position;
                    Vector3 pointB = tile.Position;
                    string s = $"A {movedPiece.GetPieceType(true)} moved from [{pointA.x},{pointA.y}] to [{pointB.x},{pointB.y}]<br> Delta: [{delta.x},{delta.y}]";
                    UpdateDescription(s);
                }
                else
                {
                    UpdateDescription("Null Point A");
                }
            }
            catch (Exception e)
            {
                UpdateDescription(e.ToString().Substring(startIndex: 0, length: 200));
            }
        }

        public override void Trigger()
        {
            //Skip the enemy turn
            SingletonMonoBehaviour<EnemyManager>.Instance.SkipTurn();
            //BOING!
            this.VisualEffect();
        }

        // DEBUG: Make sure PointAManagers reset logic works
        private void Reset(State state)
        {
            // Ignore any state resuming from a pause
            if
            (
                SingletonMonoBehaviour<GameManager>.Instance.PreviousState == State.PAUSE ||
                SingletonMonoBehaviour<GameManager>.Instance.PreviousState == State.RUN_INFO
            )
            {
                return;
            }

            // At the start of a game, add every piece on the board to the pieceTracker
            if (state == State.INGAME && SingletonMonoBehaviour<GameManager>.Instance.PreviousState == State.BOARD_PLACEMENT)
            {
                UpdateDescription("Game Start");
            }

            // At the end of a game, clearout the pieceTracker
            if (state == State.WIN || state == State.RESULT)
            {
                UpdateDescription("Game End");
            }
        }

        //DEBUG: Rewrite this gambit's description to some helpful information
        private void UpdateDescription(string s)
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


            gambitNode[$"recursion_description"] = s;
        }
    }
}
