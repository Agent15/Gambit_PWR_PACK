using System.Collections;
using System.Collections.Generic;
using Blukulele.CHE;
using UnityEngine;

namespace Gambonanza.PrismsGambit
{
    /// <summary>
    /// Prism's Gambit: After modifying a tile, modify another random tile.
    ///
	/// This gambit listens for the placement of a modified tile, then runs 2 executions of 
	/// </summary>
    public sealed class GambitPrism : BaseGambit
    {
        private bool canPlace = true;
        private void Start()
        {
            // After every tile modification, execute the trigger method.
            TileManager.Instance.OnModifyTile += Trigger;
        }

        private void OnDestroy()
        {
            // Unassign action calls
            TileManager.Instance.OnModifyTile -= Trigger;
        }

        public override void Trigger()
        {
            // Place the new tile after a short delay
            base.StartCoroutine(CO_PrismPlace(0.5f));
        }

        private IEnumerator CO_PrismPlace(float delay)
        {
            if (canPlace)
            {
                // Prevent this gambit from making other tiles for now
                canPlace = false;
                yield return new WaitForSeconds(delay);
                // If there's at least one blank tile on the board...
                TileBehaviour tile = GetEmptyTile();
                if (tile is not null)
                {
                    // BOING!
                    VisualEffect();
                    // Use an int to determine the tile type to modify
                    // (I know there's a TileType enum, but it basically does nothing -_-)
                    int type = UnityEngine.Random.Range(0, 5);
                    // Execute the respective modification
                    switch (type)
                    {
                        case 0:
                            tile.TurnToGold(false, null);
                            break;
                        case 1:
                            tile.TurnToBenediction(false, null);
                            break;
                        case 2:
                            tile.TurnToShield(false, null);
                            break;
                        case 3:
                            tile.TurnToHunter(false, null);
                            break;
                        case 4:
                            tile.TurnToPhantom(false, null);
                            break;
                    }
                }
                else
                {
                    // Hint that there aren't any blank tiles on the board
                    this.m_FeedbackIncrementor.Spawn("Board\nfull");
                }
                // Wait for a moment, then enable this gambit again
                yield return new WaitForSeconds(1f);
                canPlace = true;
            }
        }

        private TileBehaviour GetEmptyTile()
        {
            // Define a list to fill with every empty tile
            List<TileBehaviour> emptyTiles = new();
            // Define the current length of the board
            int boardLength = Mathf.Min(8, ChessDataManager.Instance.CurrentWave / 5 + 5);
            // For every "row on the board"...
            for (int i = 0; i < BoardManager.Instance.Board.GetLength(0); i++)
            {
                // For every "tile" in that "row"...
                for (int j = 0; j < boardLength; j++)
                {
                    // Define this tile
                    TileBehaviour tile = BoardManager.Instance.Board[i, j];
                    // If this tile isn't modified and is on-screen, add it to the list
                    if (!tile.IsModified() && !tile.HasFell)
                        emptyTiles.Add(tile);
                }
            }
            // If the list is empty, return nothing
            if (emptyTiles.Count == 0)
                return null;
            // Otherwise, return a random tile in the list
            else
            {
                return emptyTiles[UnityEngine.Random.Range(0, emptyTiles.Count)];
            }
        }
    }
}
