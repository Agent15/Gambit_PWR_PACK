using System;
using System.Collections;
using System.Collections.Generic;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.JumperCablesGambit
{
    /// <summary>
    /// JumperCables' Gambit: After every move, 1/5 chance to trigger one of your gambits unconditionally.
    /// 
    /// This gambit introduces a new mechanic of triggering other gambits the player currently owns
    /// </summary>
    public sealed class GambitJumperCables : BaseGambit
    {
        private System.Random pick = new();
        private void Start()
        {
            SelectionManager.Instance.OnMove += CO_Trigger;
        }

        private void OnDestroy()
        {
            SelectionManager.Instance.OnMove -= CO_Trigger;
        }

        // This is an intermediary between the required arguments of the Trigger and OnMove methods
        private void CO_Trigger(BasePieceBehaviour movedPiece, TileBehaviour tile)
        {
            Trigger();
        }

        public override void Trigger()
        {
            // Roll a 1/5 chance
            if (!SingletonMonoBehaviour<ChanceManager>.Instance.ComputeChance((float)1, (float)5, "JUMPER_CABLE_OCCURRANCE"))
			{
                // End now if the chance fails
				this.m_Gambit.Nope();
				return;
			}

            // Check if there is any gambit slot that isn't empty or Jumper Cables
            if(!HasAnotherGambit())
            {
                //There's only Jumper Cables. Cancel the trigger
                return;
            }

            //BOING!
            VisualEffect();
            // Collect every instance of BaseGambit and trigger one
            BaseGambit[] baseGambits = FindObjectsByType<BaseGambit>();
            BaseGambit selectedGambit = baseGambits[pick.Next(baseGambits.Length)];
            selectedGambit.Trigger();
        }

        private bool HasAnotherGambit(){
            List<GambitPlaceBehaviour> gambitStock = new List<GambitPlaceBehaviour>(GambitManager.Instance.GambitPlaces);
            foreach(GambitPlaceBehaviour g in gambitStock)
            {
                if(g.CurrentGambit == null || g.CurrentGambit.Info.ID.Equals("jumper-cable"))
                {
                    continue;
                }
            return true;
            }
            return false;
        }
    }
}
