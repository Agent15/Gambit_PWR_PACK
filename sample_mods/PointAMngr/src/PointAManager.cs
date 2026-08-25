using Blukulele.Core;
using Blukulele.CHE;
using System.Collections.Generic;
using UnityEngine;

namespace Gambonanza.PointAMngr
{
    /// <summary>
    /// Class: PointAManager
    ///
    /// In chess, every piece moves from Point A to Point B.
    /// Gambonanza's BasePieceBehaviour class only stores the info of point B.
    /// That's annoying. That's what this class is for.
    ///
    /// PointAManager has attributes to store the original tile info of the
    /// last piece to move (both player end enemy) and a method to calculate
    /// the diatance between them.
    ///
    /// This will allow in-game behaviors like:
    /// "If a piece moves more than three spaces..."
    /// "If a piece moves off a trap tile..."
    /// "...crumble that pieces original tile"
    /// The possibilities are endless ;)
    ///
    /// UPDATE: This class now resets its pieceTracker list every move.
    /// The previous version did not track the first move of pieces that
    /// were generated mid-game (promotion, invoker, etc.)
    /// </summary>
    public class PointAManager
    {
        // Declare the attributes for the original tiles of the last player and enemy move.
        //
        // *WARNING* These values are nullable. A null PointA implies that this
        // class wasn't initialized when that piece moved. I recommend calling
        // PointAManager.Instance.InstantFill() on the start of your dependent class
        public TileBehaviour PlayerPointA = null;
        public TileBehaviour EnemyPointA = null;

        // Declare a private list of every piece on the board and the tile it's currently
        // standing on. We'll use this as a lookup table for every piece after it moves
        private List<(BasePieceBehaviour piece, TileBehaviour tile)> pieceTracker = new();

        // Declare a private singleton instance and a pbulic read-only substitute
        // This class technically works like a singleton, but probably isn't best-practice ._.
        private static PointAManager m_instance;
        public static PointAManager Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new PointAManager();
                    // This class operates outside of the base game's usual managers, so we need to start it manually.
                    m_instance.Start();
                }
                return m_instance;
            }
        }
        private void Start()
        {
            // Execute UpdatePlayerMove on every player move
            SelectionManager.Instance.OnMove += UpdatePlayerMove;
            // Execute UpdateEnemyMove on every enemy move
            EnemyManager.Instance.OnMove += UpdateEnemyMove;
            // Execute Reset on certain state changes
            GameManager.Instance.onStateChanged += Reset;
        }

        private void OnDestroy()
        {
            // Unassign action calls
            SelectionManager.Instance.OnMove -= UpdatePlayerMove;
            EnemyManager.Instance.OnMove -= UpdateEnemyMove;
            GameManager.Instance.onStateChanged -= Reset;
        }

        // Executes after every player move to lookup that piece in the pieceTracker, and
        // assign its original tile to PlayerPointA.
        //
        // NOTE: If your dependant class also triggers on every move, I recommend adding a
        // slight IEnumerator delay to let this class update first. 0.1 secs should be fine.
        private void UpdatePlayerMove(BasePieceBehaviour argPiece, TileBehaviour argTile)
        {
            try
            {
                // Assign this piece's original tile to PlayerPointA
                TileBehaviour target = pieceTracker.Find(p => p.piece == argPiece).tile;
                PlayerPointA = target;
            }
            catch
            {// This shouldn't happen
                Debug.Log("PointAManager didn't see that piece");
                PlayerPointA = null;
            }
            // In any case, refresh the pieceTracker list
            InstantFill();
        }

        // Duplicates the behavior of UpdatePlayerMove, but for EnemyPointA
        //
        // NOTE: If your dependant class also triggers on every move, I recommend adding a
        // slight IEnumerator delay to let this class update first. 0.1 secs should be fine.
        private void UpdateEnemyMove(BasePieceBehaviour argPiece, TileBehaviour argTile)
        {
            try
            {
                // Assign this piece's original tile to PlayerPointA
                TileBehaviour target = pieceTracker.Find(p => p.piece == argPiece).tile;
                EnemyPointA = target;
            }
            catch
            {// This shouldn't happen
                Debug.Log("PointAManager didn't see that piece");
                EnemyPointA = null;
            }
            // In any case, refresh the pieceTracker list
            InstantFill();
        }

        // Clears out the pieceTracker list and populates it with every piece on the board at the start of a game
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

            // At the start of a game, reset the pieceTracker list
            if (state == State.INGAME && SingletonMonoBehaviour<GameManager>.Instance.PreviousState == State.BOARD_PLACEMENT)
            {
                InstantFill();
            }
        }

        // This method was originally for edge cases, but now it's used to reset the pieceTracker 
        // after every move. I found out after making this class that Blukulele does something very
        // similar when calculating the enemy's next move. If I could do it all over, I'd rename this
        // method to Refresh(), but I already made dependants that call InstantFill() :/
        public void InstantFill()
        {
            pieceTracker.Clear();
            foreach (BasePieceBehaviour piece in MonoBehaviour.FindObjectsByType<BasePieceBehaviour>())
            {
                pieceTracker.Add((piece, piece.CurrentTile));
            }
        }

        // A static bit of logic to determine the distance between any two tiles.
        // This can be used just by calling PointAManager.GetDelta(PointA, PointB)
        //
        // Returns: An integer tuple of the change in the two tiles' coordinates (horizontal and vertical)
        //
        // If this method returns...    | deltaY > 0 | deltaY < 0 | deltaX > 0 | dletaX < 0 |
        // This implies a piece moved...|   North    |   South    |    East    |    West    |
        public static (int deltaX, int deltaY) GetDelta(TileBehaviour pointA, TileBehaviour pointB)
        {
            int resultX = Mathf.RoundToInt(pointB.Position.x - pointA.Position.x);
            int resultY = Mathf.RoundToInt(pointB.Position.y - pointA.Position.y);
            return (resultX, resultY);
        }
    }
}