using System;
using System.Collections;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;
using DG.Tweening;

namespace Gambonanza.JunksGambit
{
    /// <summary>
    /// WIP
    /// </summary>
    public sealed class GambitJunk : BaseGambit
    {
        private System.Random pick = new();
        private int listLength = JunksGambitBuild.descriptions.Length;
        private void Start()
        {
            // Find the GambitBehaviour counterpart of this gambit
            this.m_Gambit = base.GetComponentInParent<GambitBehaviour>();

            //DEBUG: Execute this function every time a piece in the stock is clicked
            SelectionManager.Instance.OnSelectStockPiece += Boing;
        }

        private void OnDestroy()
        {}

        //DEBUG: Updates the gambit's description and displays it
        private void Boing(BasePieceBehaviour x)
        {
            int p = pick.Next(listLength);
            this.m_Gambit.Info.GambitDescription = $"junk{p}_description";
            string feedback = m_Gambit.Info.GambitDescription;
            this.m_FeedbackIncrementor.Spawn(feedback);
			this.m_FeedbackIncrementor.IncrementSound(0f);
        }

        public override void Trigger()
        {
            VisualEffect();
        }
    }
}
