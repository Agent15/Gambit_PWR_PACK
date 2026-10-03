using System;
using System.Collections;
using System.Collections.Generic;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;
using DG.Tweening;
using GamboMoves;
//using UnityEngine.UI;

namespace Gambonanza.BougiesGambit
{
    /// <summary>
    /// Bougie's Gambit: Every time you spend money, earn $1.
    /// 
    /// Vanilla Gambonanza has an action that executes every time the player spends money (including strains)
    /// This gambit piggy-backs off of that with some money-earning logic pulled from Spartain's Gambit
    ///
    /// WIP: This is an unreleased version with unsuccessful calls to the GamboMoves API.
    /// </summary>
    public sealed class GambitBougie : BaseGambit
    {
        private void Start()
        {
            //Assign this gambit's Trigger method to the ChessDataManager's OnCoinDecreased action
            //ChessDataManager.Instance.OnBoughtSomething += Behave;
            ChessDataManager.Instance.OnCoinDecreased += Trigger;

            //GAMBOMOVES
            MoveApi.MoveHookManager.pawn_move_hook_enabled = true;
            MoveApi.MoveHookManager.pawn_move_hook += AddPawnMoves;
            MoveApi.Init();
        }

        private void OnDestroy()
        {
            //Unassign this gambit's Trigger method from the ChessDataManager's OnCoinDecreased action
            ChessDataManager.Instance.OnCoinDecreased -= Trigger;

            // GAMBOMOVES
            MoveApi.MoveHookManager.pawn_move_hook -= AddPawnMoves;
        }

        public override void Trigger()
        {
            //Update the dollar count
            SingletonMonoBehaviour<ChessDataManager>.Instance.IncreaseCoin(1);
            //Generate floating money symbols
            SingletonMonoBehaviour<MoneyAnimationManager>.Instance.SpawnMoney(base.transform, 1);
            //BOING!
            this.VisualEffect();
        }

        //GAMBOMOVES
        private void AddPawnMoves(List<TileBehaviour> moves, BasePieceBehaviour piece)
        {
            var grid = MoveApi.NewGrid();// creates a new move grid of teh right size
            MoveApi.SetMove(grid, 0, 2, MoveApi.MoveType.MoveOrCapture);// helper to change the array using relative offsets

            var extraMoves = MoveApi.MoveBuilder.GenerateMovesData(piece, grid);// generate the tile array to add to the <moves> fromthe grid
            var totalMoves = MoveApi.MoveBuilder.CombineMoveLists(moves, extraMoves);//combine the arrays then clear and refill the moves with it
            moves.Clear();
            moves.AddRange(totalMoves);
        }
    }
}
