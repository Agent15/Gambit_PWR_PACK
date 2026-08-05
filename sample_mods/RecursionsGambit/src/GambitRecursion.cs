using System;
using System.Collections;
using System.Collections.Generic;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;
using DG.Tweening;

namespace Gambonanza.RecursionsGambit
{
    /// <summary>
    /// Recursion's Gambit: Moving the same piece, the same distance,
    /// in the same direction as your last move skips the enemy turn
    ///
    /// This gambit combines the turn-skipping behavior of Mime's Gambit
    /// With some custom logic to determine the direction and distance of piece moves
	///
	/// WIP: This gambit tends to not trigger unexpectedly around crumbled tiles. Still looking for a solution
    /// </summary>
    public sealed class GambitRecursion : BaseGambit
    {
        // Declare a set of variables to remember the last player move
        private int lastDeltaX = 0, lastDeltaY = 0;
        private BasePieceBehaviour lastPiece;
        // I also created a simple class to store a list of each of the player's pieces' last moves
        private List<SimpleCoordinate> lastMoves = new();
        
        private void Start()
        {
            //Assign this classes Behave() and Cleanup() methods to the game's actions
            SelectionManager.Instance.OnMove += Behave;
            GameManager.Instance.onStateChanged += Cleanup;
        }

        private void OnDestroy()
        {
            //Unssign this classes Behave() and Cleanup() methods from the game's actions
            SelectionManager.Instance.OnMove -= Behave;
            GameManager.Instance.onStateChanged += Cleanup;
            //Clean out the gambit's move history
            lastMoves.Clear();
            lastDeltaX = 0;
            lastDeltaY = 0;
        }

        //Trigger the gambit effect if this move is equal to last move, update the last move otherwise
        private void Behave(BasePieceBehaviour movedPiece, TileBehaviour tile)
        {
            //Convert the piece move into a set of x and y coordinate differences

            //If this is the first move...
            if(!lastMoves.Exists(p => p.piece == movedPiece))
            {
                //Add this piece to the list
                lastMoves.Add(new SimpleCoordinate(movedPiece,
                    (int)movedPiece.StartingTile.Position.x,
                    (int)movedPiece.StartingTile.Position.y));
            }
            //Load this piece's last move and calculate the change in coordinates
            SimpleCoordinate lastMove = lastMoves.Find(p => p.piece == movedPiece);
            int deltaX = (int)movedPiece.CurrentTile.Position.x - lastMove.x;
            int deltaY = (int)movedPiece.CurrentTile.Position.y - lastMove.y;

            if(movedPiece == lastPiece && deltaX == lastDeltaX && deltaY == lastDeltaY)
            {
                //This move was the same as the last move
                Trigger();
            } 
            else
            {
                //This move was different. Update the last move
                lastPiece = movedPiece;
                lastDeltaX = deltaX;
                lastDeltaY = deltaY;
            }
            //Update this piece's last move
            lastMove.x = (int)movedPiece.CurrentTile.Position.x;
            lastMove.y = (int)movedPiece.CurrentTile.Position.y;
        }

        public override void Trigger()
        {
            //Skip the enemy turn
			SingletonMonoBehaviour<EnemyManager>.Instance.SkipTurn();
            //BOING!
            this.VisualEffect();
        }

        public void Cleanup(State state)
        {
            if(GameManager.Instance.CurrentState == State.SHOP || GameManager.Instance.CurrentState == State.RESULT)
            {
                lastMoves.Clear();
                lastDeltaX = 0;
                lastDeltaY = 0;

                //DEBUG: Indicate a reset
                string feedback = "Reset";
                this.m_FeedbackIncrementor.Spawn(feedback);
			    this.m_FeedbackIncrementor.IncrementSound(0f);
            }
        }
    }
}
