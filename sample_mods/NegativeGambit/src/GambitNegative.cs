using System;
using System.Collections;
using System.Collections.Generic;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.NegativeGambit
{
    /// <summary>
    /// Negative Gambit: Other Gambits can be placed on top of this
	///
	/// </summary>
    public sealed class GambitNegative : BaseGambit
    {
        GambitPlaceBehaviour myPlace = null;
        private void Start()
        {
            SelectionManager.Instance.OnGambitSelection += Trigger;
		}

        private void OnDestroy()
        {
            SelectionManager.Instance.OnGambitSelection -= Trigger;
        }

        public override void Trigger()
        {
            List<GambitPlaceBehaviour> gambitStock = new List<GambitPlaceBehaviour>(GambitManager.Instance.GambitPlaces);
            foreach(GambitPlaceBehaviour g in gambitStock)
            {
                if(g.CurrentGambit is not null && g.CurrentGambit.Info.ID.Equals("negative"))
                {
                    myPlace = g;
                    break;
                }
            }
            if(myPlace is not null)
            {
                myPlace.CurrentGambit = null;
            }
        }
    }
}
