using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gambonanza.GameUI
{
    /// <summary>
    /// Hierarchy navigation primitives. Public so mods can write their own clone-and-strip
    /// patches against the game without re-implementing these.
    /// </summary>
    public static class Hierarchy
    {
        /// <summary>
        /// Returns the chain of child indices from <paramref name="ancestor"/> down to
        /// <paramref name="descendant"/>. Empty list = same transform. Null = not actually
        /// an ancestor. Use with <see cref="NavigatePath"/> on a clone of the ancestor.
        /// </summary>
        public static List<int> PathFromAncestor(Transform ancestor, Transform descendant)
        {
            if (ancestor == null || descendant == null) return null;
            var stack = new Stack<int>();
            var cur = descendant;
            while (cur != null && cur != ancestor)
            {
                stack.Push(cur.GetSiblingIndex());
                cur = cur.parent;
            }
            return cur == ancestor ? stack.ToList() : null;
        }

        /// <summary>
        /// Walks <paramref name="path"/> from <paramref name="start"/>. Returns null if
        /// any step is out of range - happens when sibling indices have shifted because
        /// something was destroyed underneath.
        /// </summary>
        public static Transform NavigatePath(Transform start, List<int> path)
        {
            if (start == null || path == null) return null;
            var cur = start;
            foreach (var idx in path)
            {
                if (idx < 0 || idx >= cur.childCount) return null;
                cur = cur.GetChild(idx);
            }
            return cur;
        }

        /// <summary>
        /// Deepest transform that is an ancestor of every input. Null if they share none.
        /// </summary>
        public static Transform FindCommonAncestor(IList<Transform> transforms)
        {
            if (transforms == null || transforms.Count == 0) return null;
            var ancestors = new HashSet<Transform>();
            for (var t = transforms[0]; t != null; t = t.parent) ancestors.Add(t);
            for (int i = 1; i < transforms.Count; i++)
            {
                var found = new HashSet<Transform>();
                for (var t = transforms[i]; t != null; t = t.parent)
                    if (ancestors.Contains(t)) found.Add(t);
                ancestors = found;
                if (ancestors.Count == 0) return null;
            }
            return ancestors.OrderByDescending(DepthOf).FirstOrDefault();
        }

        /// <summary>Depth from the scene root. 0 = root.</summary>
        public static int DepthOf(Transform t)
        {
            int d = 0;
            while (t != null && t.parent != null) { t = t.parent; d++; }
            return d;
        }

        /// <summary>Depth-first search by GameObject name. Null if not found.</summary>
        public static Transform FindChildByName(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var hit = FindChildByName(root.GetChild(i), name);
                if (hit != null) return hit;
            }
            return null;
        }

        /// <summary>
        /// Find the first live MonoBehaviour whose <see cref="System.Type.FullName"/> matches.
        /// Searches inactive objects too (Resources.FindObjectsOfTypeAll). Returns null if none.
        /// </summary>
        public static MonoBehaviour FindByTypeFullName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return null;
            var all = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            for (int i = 0; i < all.Length; i++)
            {
                var mb = all[i];
                if (mb == null) continue;
                if (mb.GetType().FullName == fullName) return mb;
            }
            return null;
        }
    }
}
