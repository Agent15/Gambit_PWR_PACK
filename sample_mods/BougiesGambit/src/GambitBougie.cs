using System;
using System.Collections;
using System.Collections.Generic;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;
using DG.Tweening;
//using UnityEngine.UI;

namespace Gambonanza.BougiesGambit
{
    /// <summary>
    /// Bougie's Gambit: Every time you spend money, earn $1.
    /// 
    /// Vanilla Gambonanza has an action that executes every time the player spends money (including strains)
    /// This gambit piggy-backs off of that with some money-earning logic pulled from Spartain's Gambit
    /// </summary>
    public sealed class GambitBougie : BaseGambit
    {
        private void Start()
        {
            //Assign this gambit's Trigger method to the ChessDataManager's OnCoinDecreased action
            //ChessDataManager.Instance.OnBoughtSomething += Behave;
            ChessDataManager.Instance.OnCoinDecreased += Trigger;
        }

        private void OnDestroy()
        {
            //Unassign this gambit's Trigger method from the ChessDataManager's OnCoinDecreased action
            ChessDataManager.Instance.OnCoinDecreased -= Trigger;
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
    }
}
