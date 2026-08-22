using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;
using DG.Tweening;

namespace Gambonanza.MysterysGambit
{
    /// <summary>
    /// Mystery's Gambit: Mimics the effects of three random gambits.
    /// 
    /// 
    /// </summary>
    public sealed class GambitMystery : BaseGambit
    {
        // Define the three child gambits for this gambit to mimic.
        private BaseGambit[] children = new BaseGambit[3];
        // Define a list of every child class of BaseGambit and a randomizer to select from it
        private Type[] allGambits = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(BaseGambit).IsAssignableFrom(type) && type != typeof(BaseGambit))
                .ToArray();
        private System.Random pick = new();
        private void Start()
        {
            // Populate the children list with three random instances of BaseGambit
            for (int x = 0; x < children.Length; x++)
            {
                children[x] = CreateGambitInstance(allGambits[pick.Next(allGambits.Length)]);
            }
            SelectionManager.Instance.OnPawnPromotionAsMoved += ShowcaseMode;//Shhhhh
        }

        private void OnDestroy()
        {
            // Delete each child gambit
            for (int x = 0; x < children.Length; x++)
            {
                Destroy(children[x]);
                children[x] = null;
            }
            SelectionManager.Instance.OnPawnPromotionAsMoved -= ShowcaseMode;//Nothing to see here ;)
        }

        // In case this gambit is triggered externally, trigger every child gambit underneath it
        // (I'm gonna have some fun with Jumper Cables >:)
        public override void Trigger()
        {
            foreach (BaseGambit child in children)
            {
                if (child is not null)
                {
                    child.Trigger();
                }
            }
        }

        // I'm not gonna lie. This was vibecoded. But the idea of this method is to generate only an instance 
        // of a BaseGambit child class without earning a "physical" gambit and adding it to the stock.
        public BaseGambit CreateGambitInstance(Type childType, GameObject targetObject = null)
        {
            // 1. Ensure the provided type actually inherits from BaseGambit
            if (!typeof(BaseGambit).IsAssignableFrom(childType) || childType == typeof(BaseGambit))
            {
                Debug.LogError($"{childType.Name} does not inherit from BaseGambit!");
                return null;
            }

            // 2. Decide where to attach the component (create a new GameObject if none provided)
            GameObject host = targetObject != null ? targetObject : new GameObject($"gambit_{childType.Name}");

            // 3. Add the component using the System.Type object
            BaseGambit newGambit = (BaseGambit)host.AddComponent(childType);

            // 4. Locate or create the required dependencies on the GameObject
            // NOTE: I modified the following line to assign Mystery's Gambit's GambitBehaviour
            // component to the child gambit instance. That way every child gambit's fedback
            // animations are anchored to Mystery's Gambit's sprite. 
            GambitBehaviour behavior = this.m_Gambit;
            if (behavior == null) behavior = host.AddComponent<GambitBehaviour>();

            GambitFeedbackIncrementor incrementor = host.GetComponent<GambitFeedbackIncrementor>();
            if (incrementor == null) incrementor = host.AddComponent<GambitFeedbackIncrementor>();

            // 5. Bypass protected modifiers using reflection to assign fields manually
            // BindingFlags.NonPublic | BindingFlags.Instance grabs protected/private instance variables
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

            FieldInfo gambitField = typeof(BaseGambit).GetField("m_Gambit", flags);
            FieldInfo feedbackField = typeof(BaseGambit).GetField("m_FeedbackIncrementor", flags);

            // Assign the references to the newly created instance
            gambitField?.SetValue(newGambit, behavior);
            feedbackField?.SetValue(newGambit, incrementor);

            return newGambit;
        }
        // When I make a showcase for this mod, I want to know what child gambits I'm working with
        private void ShowcaseMode(BasePieceBehaviour piece, TileBehaviour tile)
        {
            if (piece.GetPieceType() == PieceType.PAWN && tile.Position.y < 0)
            {
                for (int x = 0; x < children.Length; x++)
                {
                    if (children[x] is not null) Destroy(children[x]);
                }
                children[0] = CreateGambitInstance(typeof(GambitMime));
                children[1] = CreateGambitInstance(typeof(Violet_Gambit));
                children[2] = CreateGambitInstance(typeof(MissignoGambit));
                this.m_FeedbackIncrementor.Spawn(";)");
			    this.m_FeedbackIncrementor.IncrementSound(0f);
            }
        }
    }
}
