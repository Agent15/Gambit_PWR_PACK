using System;
using System.Collections;
using System.Collections.Generic;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.NullsGambit
{
    /// <summary>
    /// Null's Gambit: ________________________
	///
	///
    public sealed class GambitNull : BaseGambit
    {
        private void Start()
        {
		}

        private void OnDestroy()
        {
        }

        public override void Trigger()
        {
            // BOING!
            VisualEffect();
        }
    }
}
