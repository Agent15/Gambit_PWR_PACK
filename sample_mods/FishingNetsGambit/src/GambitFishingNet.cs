using System;
using System.Collections;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;
using DG.Tweening;

namespace Gambonanza.FishingNetsGambit
{
    /// <summary>
    /// Fishing Net's Gambit: When an enemy piece is trapped, skip the enemy's turn.
    /// 
    /// This gambit's design is simple. It listens for any trap-like
    /// action call and responds by skipping the enemy's turn.
    /// Just because an idea is simple, doesn't mean it isn't good <3
    /// </summary>
    public sealed class GambitFishingNet : BaseGambit
    {
        private void Start()
        {
            TileManager.Instance.OnHunterTileUsed += TileTrigger;
            PieceManager.Instance.OnTrap += PieceTrigger;
        }

        private void OnDestroy()
        {
            TileManager.Instance.OnHunterTileUsed -= TileTrigger;
            PieceManager.Instance.OnTrap -= PieceTrigger;
        }

        //This is an intermediary between the argument requirements of the OnHunterTileUsed and Trigger methods
        private void TileTrigger(BasePieceBehaviour x, TileBehaviour y)
        {
            Trigger();
        }

        //This is an intermediary between the argument requirements of the OnHunterTileUsed and Trigger methods
        private void PieceTrigger(BasePieceBehaviour x)
        {
            Trigger();
        }

        public override void Trigger()
        {
            //Skip the enemy turn
			SingletonMonoBehaviour<EnemyManager>.Instance.SkipTurn();
            // BOING!
            VisualEffect();
        }
    }
}
