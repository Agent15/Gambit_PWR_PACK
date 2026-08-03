using System;
using System.Collections;
using System.Collections.Generic;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;
using DG.Tweening;
//using UnityEngine.UI;

namespace Gambonanza.LobbyistsGambit
{
    /// <summary>
    /// WIP
    /// </summary>
    public sealed class GambitLobbyist : BaseGambit
    {
        private void Start()
        {
            //Assign the SellManager's OnSellPiece to this classes OnSell() method
            SellManager instance = SingletonMonoBehaviour<SellManager>.Instance;
			instance.OnSellPiece = (Action<BasePieceBehaviour>)Delegate.Combine(instance.OnSellPiece, new Action<BasePieceBehaviour>(this.OnSell));
        }

        private void OnDestroy()
        {
            SellManager instance = SingletonMonoBehaviour<SellManager>.Instance;
            instance.OnSellPiece = (Action<BasePieceBehaviour>)Delegate.Remove(instance.OnSellPiece, new Action<BasePieceBehaviour>(this.OnSell));
        }

        //This method is an intermediary between the SellManager's OnSellPiece type requirements
        //and the required return value of our OnSell function
        private void OnSell(BasePieceBehaviour soldPiece)
		{
            base.StartCoroutine(this.CheckAndTrigger(soldPiece));
		}

        private IEnumerator CheckAndTrigger(BasePieceBehaviour soldPiece)
		{
            //This wait is required after selling a piece. If it triggered immediately,
            //it would interrupt the OnSellPiece process and the sold piece wouldn't dissapear
            //(Don't ask me how I know ._.)
            yield return new WaitForSeconds(0.5f);

            //Only trigger if we're in the middle of a game and the sold piece is a "king"
            if (GameManager.Instance.CurrentState == State.INGAME && (soldPiece.GetPieceType() == PieceType.KING || SingletonMonoBehaviour<GambitManager>.Instance.AnarchistEnable))
            {
			    Trigger();
            }
		}

        public override void Trigger()
        {
            //Collect the set of all black non-elite pieces
            List<BasePieceBehaviour> victims = new ();
            foreach (BasePieceBehaviour piece in FindObjectsByType<BasePieceBehaviour>())
			{
                if(piece.PieceColor == PieceColor.BLACK && !piece.EnemyAbilityModifier.IsBoss)
                {
                    victims.Add(piece);
                }
            }
            if(victims.Count == 0)
            {
                return;
            }
            //Select a random piece from the set
            System.Random Pick = new();
            BasePieceBehaviour target = victims[Pick.Next(victims.Count)];
            //Capture the piece
            PerformLobbyistKill(target, target.CurrentTile);
            //BOING!
            VisualEffect();
        }

        //Deletes a piece from the board without an attacker, and notifies
        //every necessary manager to delete the piece from the board.
        //Credit to Bentrd for laying the groundwork with Kamikaze's gambit
        private void PerformLobbyistKill(BasePieceBehaviour enemy, TileBehaviour tile)
        {
            enemy.IsDead = true;

            var pieceManager = SingletonMonoBehaviour<PieceManager>.Instance;
            if (pieceManager != null)
                pieceManager.UnregisterPiece(enemy);

            var enemyManager = SingletonMonoBehaviour<EnemyManager>.Instance;
            if (enemyManager != null)
                enemyManager.EnemyPieces.Remove(enemy);

            if (enemy.VisualEffect != null)
                enemy.VisualEffect.Disappear(0.25f);

            enemy.enabled = false;
            UnityEngine.Object.Destroy(enemy.gameObject, 0.6f);

            var chessData = SingletonMonoBehaviour<ChessDataManager>.Instance;
            if (chessData != null)
                chessData.PiecesCaptured++;

            try { enemy.CaptureEffect(); } catch { }
        }
    }
}
