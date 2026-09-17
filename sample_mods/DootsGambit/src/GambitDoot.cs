using Blukulele.CHE;
using Blukulele.Core;

namespace Gambonanza.DootsGambit
{
    /// <summary>
    /// Doot's Gambit: Moving adjacent to an enemy piece has a 1/7 chance to capture it.
	///
    /// This gambit combines the adjacent piece checking of Evangelist's gambit with the
    /// Unconditional piece capture of Lobbyist's gambit.
	/// </summary>
    public sealed class GambitDoot : BaseGambit
    {
        private void Start()
        {
            SelectionManager.Instance.OnMove += Behave;
		}

        private void OnDestroy()
        {
            SelectionManager.Instance.OnMove -= Behave;
        }

        private void Behave(BasePieceBehaviour piece, TileBehaviour tile)
        {
            // Define a set of boolean flags to determine the visual effect to use later
            bool tryFlag = false, dootFlag = false;
            // For every tile adjacent to this piece...
            foreach (TileBehaviour t in tile.GetNeighbourTiles())
            {
                // Skip this step if this tile has
                // - No piece
                // - A player's piece
                // - An elite or stasis piece
                if (t.Piece is not null 
                && t.Piece.PieceColor == PieceColor.BLACK
                && t.Piece.EnemyAbilityModifier.CurrentState != PieceState.BOSS
                && t.Piece.EnemyAbilityModifier.CurrentState != PieceState.STASIS)
                {
                    tryFlag = true;
                    // Roll a 1/7 chance
                    if (ChanceManager.Instance.ComputeChance((float)1, (float)7, "DOOT_OCCURRANCE"))
                    {
                        // The chance passed. Capture this enemy piece 
                        PerformDootKill(t.Piece, t);
                        dootFlag = true;
                    }
                }
            }
            if (tryFlag)
            {
                // This move was adjacent to atleast one enemy piece
                if (dootFlag)
                {
                    // At least one piece was captured. BOING!
                    VisualEffect();
                }
                else
                {
                    // Every attempted chance failed. Play the "No luck" animation
                    this.m_Gambit.Nope();
                }
            }
        }

        private void PerformDootKill(BasePieceBehaviour piece, TileBehaviour tile)
        {
            // Notify other classes that this piece has been captured by itself
            var selectionManager = SelectionManager.Instance;
            if(selectionManager is not null && selectionManager.OnCapture is not null)
                selectionManager.OnCapture.Invoke(piece, piece, tile);

            // Tell this tile it doesn't have a piece on it anymore
            tile.Piece = null;

            // Mark the enemy piece as dead and disabled
            piece.IsDead = true;
            piece.enabled = false;

            // Unregister the piece with PieceManager
            var pieceManager = SingletonMonoBehaviour<PieceManager>.Instance;
            if (pieceManager != null)
                pieceManager.UnregisterPiece(piece);

            // Remove this piece from EnemyManager's list of pieces
            var enemyManager = SingletonMonoBehaviour<EnemyManager>.Instance;
            if (enemyManager != null)
                enemyManager.EnemyPieces.Remove(piece);

            // Remove the piece from the screen
            if (piece.VisualEffect != null)
                piece.VisualEffect.Disappear(0.25f);

            // Destroy this piece's game object
            UnityEngine.Object.Destroy(piece.gameObject, 0.6f);

            // Increment the PiecesCaptured counter
            var chessData = SingletonMonoBehaviour<ChessDataManager>.Instance;
            if (chessData != null)
                chessData.PiecesCaptured++;

            // Make the piece explode in a cloud of bits
            try { piece.CaptureEffect(); } catch { }

            // Play the saucy shockwave animation
            if(ShockWaveManager.Instance is not null)
                ShockWaveManager.Instance.StartWave(tile.GetWaveBehaviour());
        }

        public override void Trigger()
        {
            // BOING!
            VisualEffect();
        }
    }
}
