using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Gambonanza.MysterysGambit
{
    /// <summary>
    /// Mystery's Gambit: Mimics the effects of three random gambits.
    /// 
    /// This gambit has three primary functions:
    /// 1: When it is created, generate three instances of three random child classes of BaseGambit
    ///     and set their m_Gambit fields to point back to itself
    /// 2: Determine which GambitBehaviour instances the game would pair with that BaseGambit and
    ///     generate an instance of that class as well
    /// 3: When it is destroyed, destroy every child BaseGambit and trigger every child
    ///     GambitBehaviour's Sell() method
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
        // Some gambits trigger their selling behavior in a specialized child of the GambitBehaviour class.
        // A child's GambitBehaviour will be assigned to this gambit, so we'll need to save that logic externally.
        List<GambitBehaviour> behaviors = new();
        private void Start()
        {
            // Populate the children list with three random instances of BaseGambit
            for (int x = 0; x < children.Length; x++)
            {
                children[x] = CreateGambitInstance(allGambits[pick.Next(allGambits.Length)]);
            }
            SelectionManager.Instance.OnSelectStockPiece += ShowcaseMode;//Shhhhh
        }

        private void OnDestroy()
        {
            // Delete each child gambit's gameObject. (I'm not sure if the game does this
            // automatically at any point, so I'll wrap it in a try block just to be safe)
            try
            {
                for (int x = 0; x < children.Length; x++)
                {
                    Destroy(children[x].gameObject);
                    children[x] = null;
                }
            }
            catch { }
            // Trigger every child gambit's selling behavior
            // (For any edge cases I might've missed, I'm wrapping each of these in a try block as well)
            foreach (GambitBehaviour b in behaviors)
            {
                if (b == null) continue;

                try
                {
                    b.Sell();
                }
                catch (Exception e)
                {
                    Debug.Log($"Mystery's Gambit Failed\n{e.ToString()}");
                }
            }
            SelectionManager.Instance.OnSelectStockPiece -= ShowcaseMode;//Nothing to see here ;)
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
            if (!typeof(BaseGambit).IsAssignableFrom(childType) || childType == typeof(BaseGambit))
            {
                Debug.LogError($"{childType.Name} does not inherit from BaseGambit!");
                return null;
            }

            // 1. Host hierarchy resolution: Attach to Mystery's Gambit's GameObject if target is null 
            // to preserve UI RectTransform scaling/Canvas placement.
            GameObject host = targetObject != null ? targetObject : new GameObject($"gambit_{childType.Name}");
            if (targetObject == null)
            {
                host.transform.SetParent(this.transform, false);
            }

            // 2. Add the component instance
            BaseGambit newGambit = (BaseGambit)host.AddComponent(childType);

            // 3. Setup standard BaseGambit fields via reflection
            // NOTE: On the next few lines, we overwrite this new gambit's GambitBehaviour attribute
            // with Mystery's Gambit's GambitBehaviour, and add its true behaviour to our list.
            Type behaviorType = GetBehavior(childType);
            GambitBehaviour childBehaviour = (GambitBehaviour)host.AddComponent(behaviorType);
            behaviors.Add(childBehaviour);
            GambitBehaviour behavior = this.m_Gambit;

            GambitFeedbackIncrementor incrementor = host.GetComponent<GambitFeedbackIncrementor>();
            if (incrementor == null) incrementor = host.AddComponent<GambitFeedbackIncrementor>();

            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

            FieldInfo gambitField = typeof(BaseGambit).GetField("m_Gambit", flags);
            FieldInfo feedbackField = typeof(BaseGambit).GetField("m_FeedbackIncrementor", flags);
            FieldInfo infoField = typeof(GambitBehaviour).GetField("m_Info", flags);

            gambitField?.SetValue(newGambit, behavior);
            feedbackField?.SetValue(newGambit, incrementor);
            infoField.SetValue(childBehaviour, this.m_Gambit.Info);

            // 4. Dynamically instantiate and wire up UI Image fields
            // NOTE: This is an edge case for gambits that also display piece sprites (Violet, Cauldron, etc.)
            FieldInfo[] fields = childType.GetFields(flags);
            foreach (FieldInfo field in fields)
            {
                if ((field.FieldType == typeof(Image) || field.FieldType == typeof(Image[]))
                    && field.GetValue(newGambit) == null)
                {
                    // Create a child UI object to house the Image
                    GameObject imageObj = new GameObject($"UI_{field.Name}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    imageObj.transform.SetParent(host.transform, false);

                    var imgComponent = imageObj.GetComponent(field.FieldType);

                    // Assign the newly generated Image component to the field
                    field.SetValue(newGambit, imgComponent);
                }
            }

            return newGambit;
        }

        // I'm not gonna lie. This is also vibecoded ._.
        // The purpose if this method is to return the child class of GambitBehaviour that the game
        // would return for the inputted BaseGambit
        // For example: GetBehavior(typeof(Gambit_Eugenic)) would return EugenicsGambitBehaviour.
        private Type GetBehavior(Type gambitType)
        {
            // Try to find a GambitBehaviour implementation matching the naming convention (e.g. MissignoGambit -> MissignoBehaviour)
            string baseName = gambitType.Name.Replace("Gambit", "");

            Type behaviorType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => typeof(GambitBehaviour).IsAssignableFrom(t)
                                  && t != typeof(GambitBehaviour)
                                  && t.Name.StartsWith(baseName));

            // Fallback to base GambitBehaviour if no specialized subclass exists
            return behaviorType ?? typeof(GambitBehaviour);
        }

        // When I make a showcase for this mod, I want to know what child gambits I'm working with.
        private void ShowcaseMode(BasePieceBehaviour piece)
        {
            try
            {
                if (piece.GetPieceType() == PieceType.PAWN && ChessDataManager.Instance.Coins == 420)
                {
                    behaviors.Clear();
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
            catch (Exception e)
            {
                this.m_FeedbackIncrementor.Spawn("D:");
                this.m_FeedbackIncrementor.IncrementSound(0f);
                Debug.Log($"Mystery's Gambit Failed\n{e.ToString()}");
            }
        }
    }
}
