using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Blukulele.CHE;
using UnityEngine;

namespace Gambonanza.JudgementsGambit
{
    /// <summary>
    /// Judgement's Gambit: If a blessed piece is captured, cause the captured tile to crumble.
	///
    /// This gambit listens for EnemyManager's OnCapture call and marks the captured tile as crumbling
    /// if the captured piece was blesses, then listens for TurnManager's OnPlayerCheckIfCanPlay and
    /// drops any tile it marked as crumbling.
	/// </summary>
    public sealed class GambitJudgement : BaseGambit
    {
        TileBehaviour tileToDrop = null;
        private void Start()
        {
            // On every capture, execute the Behave method
            EnemyManager.Instance.OnCapture += Behave;
            // On every player turn check, drap any tile thid gambit caused to crumble
            TurnManager.Instance.OnPlayerCheckIfCanPlay += DropTile;
        }

        private void OnDestroy()
        {
            // Unassign actio calls
            EnemyManager.Instance.OnCapture -= Behave;
            TurnManager.Instance.OnPlayerCheckIfCanPlay -= DropTile;
            // Drop any tile this gambit caused to crumble
            base.StartCoroutine(CO_DropTile(0f));
        }

        private void Behave(BasePieceBehaviour attaker, BasePieceBehaviour victim, TileBehaviour tile)
        {
            if (victim.Modifier.IsBenediction)
            {
                //BOING!
                VisualEffect();
                // Mark the captured tile as the tile to drop
                tileToDrop = tile;
                // Signal to crumbleManager to crumble this tile
                CrumbleManager.Instance.CrumblerEffect(tile);
                tile.TileVisual.CrumblerEffect();
            }
        }

        private void DropTile()
        {
            base.StartCoroutine(CO_DropTile(0.3f));
        }

        private IEnumerator CO_DropTile(float delay)
        {
            // Wait for CrumbleManager to update first...
            yield return new WaitForSeconds(delay);
            // If there's a tile that needs to be droped...
            if (tileToDrop is not null && !tileToDrop.HasFell)
            {
                tileToDrop.Fall();
                // Grab the CrumbleManager's List of fallen tiles through reflection and add this tile to the list.
                BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
                FieldInfo tilesField = typeof(CrumbleManager).GetField("m_FellTiles", flags);
                List<TileBehaviour> fellTiles = (List<TileBehaviour>)tilesField?.GetValue(CrumbleManager.Instance);
                fellTiles.Add(tileToDrop);
                // Forget this tile
                tileToDrop = null;
            }
        }

        public override void Trigger()
        {
            // BOING!
            VisualEffect();
        }
    }
}
