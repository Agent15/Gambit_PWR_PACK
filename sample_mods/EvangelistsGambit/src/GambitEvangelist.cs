using System.Collections.Generic;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.EvangelistsGambit
{
    /// <summary>
    /// Evangelist's Gambit: Moving a bishop adjacent to an enemy piece has a 1/4 chance of
    /// transforming it into one of your pieces.
    /// 
    /// This gambit takes inspiration from the piece transformation behavior of Witch's gambit
    /// but instead transforming to a piece of the same type and different color
    /// </summary>
    public sealed class GambitEvangelist : BaseGambit
    {
        private void Start()
        {
            // On every move, execute the Behave() method
            SelectionManager.Instance.OnMove += Behave;
        }

        private void OnDestroy()
        {
            // Unassign action calls
            SelectionManager.Instance.OnMove -= Behave;
        }

        private void Behave(BasePieceBehaviour piece, TileBehaviour tile)
        {
            // We only want to check for bishop moves
            if (piece.GetPieceType() != PieceType.BISHOP)
                return;
            // Define a set of boolean flags to determine the visual effect to use later
            bool tryFlag = false, convertFlag = false;
            // For every tile adjacent to this piece...
            foreach (TileBehaviour t in tile.GetNeighbourTiles())
            {
                // Skip this step if this tile has
                // - No piece
                // - A player's piece
                // - An enemy-modified piece (I don't wanna bother with all that)
                if (t.Piece is not null && t.Piece.PieceColor == PieceColor.BLACK && t.Piece.EnemyAbilityModifier.CurrentState == PieceState.NONE)
                {
                    // End this effect early if your board is ever at max capacity. #LichNerf
                    int oshiyaBuffer = GambitManager.Instance.OshiyaGambit ? 1 : 0;
                    if (BoardManager.Instance.GetPlayerPiecesInBoardCount() >= ChessDataManager.Instance.MaxPieceOnBoard + oshiyaBuffer)
                    {
                        if (convertFlag)
                        {
                            // At least one piece was converted. BOING!
                            VisualEffect();
                        }
                        else
                        {
                            // Play a small cue that the board is at max capacity
                            this.m_FeedbackIncrementor.Spawn("Max\npieces");
                            this.m_FeedbackIncrementor.IncrementSound(0f);
                        }
                        return;
                    }
                    tryFlag = true;
                    // Roll a 1/4 chance
                    if (ChanceManager.Instance.ComputeChance((float)1, (float)4, "EVANGELIST_OCCURRANCE"))
                    {
                        // The chance passed. Convert this enemy piece 
                        PerformEvangelistConvert(t.Piece);
                        convertFlag = true;
                    }
                }
            }
            if (tryFlag)
            {
                if (convertFlag)
                {
                    // At least one piece was converted. BOING!
                    VisualEffect();
                }
                else
                {
                    // Every attempted chance failed. Play the "No luck" animation
                    this.m_Gambit.Nope();
                }
            }
        }

        // This method is an isolated sequence to convert an enemy piece into a player's piece
        // This logic is largely copy/pasted from Witch's gambit with a few tweaks
        public void PerformEvangelistConvert(BasePieceBehaviour piece)
        {
            // Create a new piece with the original piece's type and position, but with a white piece color
            BasePieceBehaviour newPiece = Instantiate<BasePieceBehaviour>(SingletonMonoBehaviour<Library>.Instance.GetPiece(piece.GetPieceType(), PieceColor.WHITE), piece.transform.position, Quaternion.identity);
            // We don't want a flashy promotion-like animation
            newPiece.GetComponent<PieceApparitionEffect>().LaunchAnimationAtStart = false;
            // The piece specific gambit visual effects are difficult to parse from C# alone,
            // so I'm adopting the transformation effect of polymorphic pieces
            newPiece.VisualEffect.MagicianEffect();
            // Register the new piece with PieceManager and copy any special modifiers (phantom, protected, etc.)
            SingletonMonoBehaviour<PieceManager>.Instance.CopyPieceCaractericstics(piece, newPiece, piece.CurrentTile);
            // Find an open space on the player's side of the board and update the new piece's starting Tile
            // Without this, a converted piece will reset to its starting location on the enemy's side of
            // the board at the end of a game
            List<TileBehaviour> allTiles = new List<TileBehaviour>(FindObjectsByType<TileBehaviour>());
            TileBehaviour openSpace = allTiles.Find(t => t.CanBeUsedForFormation && !t.IsStock);
            newPiece.StartingTile = openSpace;
            // Destroy the original piece
            Destroy(piece.gameObject);
        }
        public override void Trigger()
        {
            // BOING!
            VisualEffect();
        }
    }
}
