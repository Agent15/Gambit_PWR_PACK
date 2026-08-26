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
    /// WIP: Any child gambits with a special sell behavior throw an exception when this gambit
    /// is sold (Line 63). I haven't found the cause of the issue yet.
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
        // Some gambits trigger their selling behavior in a specialized child of the GambitBehaviour class
        // We'll assign a child's GambitBehaviour to this gambit, so we'll need to save that logic externally.
        List<GambitBehaviour> behaviors = new();
        private System.Random pick = new();
        private void Start()
        {
            string s = ""; //DEBUG
                           // Populate the children list with three random instances of BaseGambit
            for (int x = 0; x < children.Length; x++)
            {
                children[x] = CreateGambitInstance(allGambits[pick.Next(allGambits.Length)]);
                s += children[x].name + '\n';//DEBUG
            }
            SelectionManager.Instance.OnSelectStockPiece += ShowcaseMode;//Shhhhh
            UpdateDescription(s);//DEBUG
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
            try{
            foreach (GambitBehaviour b in behaviors)
            {
                if(b.GetType() != typeof (GambitBehaviour))
                {
                    b.Sell();
                }
            }
            UpdateDescription("We made it");
            SelectionManager.Instance.OnSelectStockPiece -= ShowcaseMode;//Nothing to see here ;)
            }catch (Exception e) {UpdateDescription(e.ToString());}
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
            behaviors.Add((GambitBehaviour)host.AddComponent(behaviorType));
            GambitBehaviour behavior = this.m_Gambit;

            GambitFeedbackIncrementor incrementor = host.GetComponent<GambitFeedbackIncrementor>();
            if (incrementor == null) incrementor = host.AddComponent<GambitFeedbackIncrementor>();

            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

            FieldInfo gambitField = typeof(BaseGambit).GetField("m_Gambit", flags);
            FieldInfo feedbackField = typeof(BaseGambit).GetField("m_FeedbackIncrementor", flags);

            gambitField?.SetValue(newGambit, behavior);
            feedbackField?.SetValue(newGambit, incrementor);

            // 4. FIX: Dynamically instantiate and wire up UI Image fields
            // This is an edge case for gambits that also display piece sprites (Violet, Cauldron, etc.)
            FieldInfo[] fields = childType.GetFields(flags);
            foreach (FieldInfo field in fields)
            {
                if ((field.FieldType == typeof(Image) || field.FieldType == typeof(Image[]))
                    && field.GetValue(newGambit) == null)
                {
                    // Create a child UI object to house the Image
                    GameObject imageObj = new GameObject($"UI_{field.Name}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    imageObj.transform.SetParent(host.transform, false);

                    Image imgComponent = imageObj.GetComponent<Image>();

                    // Assign the newly generated Image component to the field
                    field.SetValue(newGambit, imgComponent);
                }
            }

            return newGambit;
        }

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
        // When I make a showcase for this mod, I want to know what child gambits I'm working with
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
                    string s = "";
                    foreach (GambitBehaviour b in behaviors)
                    {
                        s += b.ToString() + '\n';
                    }
                    UpdateDescription(s);
                    this.m_FeedbackIncrementor.Spawn(";)");
                    this.m_FeedbackIncrementor.IncrementSound(0f);
                }
            }
            catch (Exception e)
            {
                UpdateDescription(e.ToString());
            }
        }
        //DEBUG
        public static void UpdateDescription(string s)
        {
            var locManager = SingletonMonoBehaviour<LocalizationManager>.Instance;
            if (locManager == null)
            {
                Debug.LogWarning("[GambitApi] LocalizationManager not found, tooltip text will be empty.");
                return;
            }

            // Force load if not cached
            var traduction = locManager.GetTraduction();
            if (traduction == null)
            {
                Debug.LogWarning("[GambitApi] GetTraduction() returned null.");
                return;
            }

            var gambitNode = traduction["gambit"];
            if (gambitNode == null)
            {
                Debug.LogWarning("[GambitApi] traduction['gambit'] node not found.");
                return;
            }

            gambitNode[$"mystery_description"] = s;
        }

        private void Handle()
        {

        }
    }
}
