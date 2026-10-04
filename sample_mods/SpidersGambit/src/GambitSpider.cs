using Blukulele.CHE;

namespace Gambonanza.SpidersGambit
{
    /// <summary>
    /// Spider's Gambit: After moving a knight, trap every piece it can capture.
	///
    /// This gambit listens to SelectionManager's OnMove call, checks if the piece is a
    /// "knight", then traps every occupied tile from the piece's GetTilesAvailable() method
	/// </summary>
    public sealed class GambitSpider : BaseGambit
    {
        private void Start()
        {
            // After every player move, execute the Behave() method.
            SelectionManager.Instance.OnMove += Behave;
		}

        private void OnDestroy()
        {
            // Unassign action calls.
            SelectionManager.Instance.OnMove -= Behave;
        }

        private void Behave(BasePieceBehaviour piece, TileBehaviour x)
        {
            // If this piece is a "knight"...
            if(piece.GetPieceType() == PieceType.KNIGHT 
                || (piece.Modifier.IsPromoted && GambitManager.Instance.PegasusActivated))
            {
                // Declare a bool variable to use for visual effects
                bool spiderFlag = false;
                // For every tile it can move to...
                foreach(TileBehaviour tile in piece.GetTilesAvailable())
                {
                    // If that tile has an enemy piece on it...
                    if(tile.Piece is not null && tile.Piece.PieceColor == PieceColor.BLACK)
                    {
                        spiderFlag = true;
                        // Trap that tile's piece
                        BasePieceBehaviour target = tile.Piece;
                        target.Modifier.TrapInstant(1);
                        // Alert other objects that a piece has been trapped
                        if(PieceManager.Instance.OnTrap is not null)
                            PieceManager.Instance.OnTrap.Invoke(target);
                    }
                }
                if(spiderFlag)
                {
                    // At least one piece was trapped. BOING!
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
