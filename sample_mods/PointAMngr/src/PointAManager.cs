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
    /// </summary>
    public class PointAManager
    {
        // Declare the attributes for the original tiles of the last player and enemy move.
        //
        // *WARNING* These values are nullable. A null PointA implies the piece that moved
        // was generated mid-game and hasnt's moved yet (Things like Landing, Lich's Gambit,
        // or Invoker pieces). The only solution I can think of for this is to scan the
        // entire board for new pieces after every move, and I don't feel like doing that ._.
        public TileBehaviour PlayerPointA = null;
        public TileBehaviour EnemyPointA = null;

        // Declare a private list of every piece on the board and the tile it's currently
        // standing on. We'll use this as a lookup table for each piece after it moves
        private List<PieceAndTile> pieceTracker = new();

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
            set { }
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
            // If the piece that moved is in the pieceTracker list...
            if (pieceTracker.Exists(p => p.piece == argPiece))
            {
                PieceAndTile target = pieceTracker.Find(p => p.piece == argPiece);
                // Assign that piece's original tile to PlayerPointA
                PlayerPointA = target.tile;
                // Update this piece's current tile in pieceTracker
                target.tile = argTile;
            }
            else
            {// This piece was created mid-game and hasn't moved yet
                PlayerPointA = null;
                //Add this piece to the pieceTracker list
                pieceTracker.Add(new PieceAndTile(argPiece, argTile));
            }
        }

        // Duplicates the behavior of UpdatePlayerMove, but for EnemyPointA
        //
        // NOTE: If your dependant class also triggers on every move, I recommend adding a
        // slight IEnumerator delay to let this class update first. 0.1 secs should be fine.
        private void UpdateEnemyMove(BasePieceBehaviour argPiece, TileBehaviour argTile)
        {
            if (pieceTracker.Exists(p => p.piece == argPiece))
            {
                PieceAndTile target = pieceTracker.Find(p => p.piece == argPiece);
                EnemyPointA = target.tile;
                target.tile = argTile;
            }
            else
            {
                EnemyPointA = null;
                pieceTracker.Add(new PieceAndTile(argPiece, argTile));
            }
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

            // At the start of a game, add every piece on the board to the pieceTracker
            if (state == State.INGAME && SingletonMonoBehaviour<GameManager>.Instance.PreviousState == State.BOARD_PLACEMENT)
            {
                // Cleanout the list
                pieceTracker.Clear();
                // Populate it with every piece on the board
                foreach (BasePieceBehaviour piece in MonoBehaviour.FindObjectsByType<BasePieceBehaviour>())
                {
                    pieceTracker.Add(new PieceAndTile(piece, piece.CurrentTile));
                }
            }
        }

        // For edge cases that would start this class in the middle of a game, you can 
        // use this method to unconditionally dump and refill the pieceTracker list
        public void InstantFill()
        {
            pieceTracker.Clear();
            foreach (BasePieceBehaviour piece in MonoBehaviour.FindObjectsByType<BasePieceBehaviour>())
            {
                pieceTracker.Add(new PieceAndTile(piece, piece.CurrentTile));
            }
        }

        // A static bit of logic to determine the distance between any two tiles. This can be used
        // just by calling PointAManager.GetDelta(PointA, PointB)
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
    /// For this class to work, I needed a mutable data type that pairs a BasePieceBehaviour to a TileBehaviour
    /// without modifying either of them directly. Tuples can't be modified after being initially assigned
    /// so I decided to make a small custom class instead.
    public class PieceAndTile
    {
        public BasePieceBehaviour piece;
        public TileBehaviour tile;
        public PieceAndTile(BasePieceBehaviour p, TileBehaviour t)
        {
            piece = p;
            tile = t;
        }
    }
}