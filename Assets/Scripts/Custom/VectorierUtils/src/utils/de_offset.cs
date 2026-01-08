using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

// -=-=-=- //

public class DeOffsetSelected : MonoBehaviour {
    // Ctrl+Shift+;
    [MenuItem("Vectorier/⚙ Utils/✥ Objects positioning/⤣ De-offset selected #-")]
    private static void DeOffset() {
        var selection = Selection.transforms;

        if (selection == null || selection.Length == 0) {
            return;
		}

        // quick lookup set of originally selected transforms
        var selectedSet = new HashSet<Transform>(selection);

        // order map: keep the Selection order; children added from parent get high index + sibling index
        var orderMap = new Dictionary<Transform, int>();

        for (int i = 0; i < selection.Length; i++) {
            orderMap[selection[i]] = i;
		}

        var targets = new List<Transform>();

        foreach (var t in selection)  {
            // If this selected object has children and none of its descendants are selected,
            // treat this as "parent-only selected" -> add all immediate children.
            if (t.childCount > 0 && !HasAnyDescendantSelected(t, selectedSet)) {
                for (int i = 0; i < t.childCount; i++) {
                    var child = t.GetChild(i);

                    if (!targets.Contains(child)) {
                        targets.Add(child);
	
                        // give a deterministic ordering for added children after selection order
                        orderMap[child] = 100000 + child.GetSiblingIndex();
                    }
                }
            } else {
                // otherwise include the selected transform itself
                if (!targets.Contains(t)) {
                    targets.Add(t);

                    if (!orderMap.ContainsKey(t)) {
						// fallback index (shouldn't happen for originally selected)
                        orderMap[t] = 1000000;
					}
                }
            }
        }

        if (targets.Count == 0) {
            return;
		}

        // Group by parent so we operate in each parent's local space separately
        var groups = targets.GroupBy(x => x.parent);

        foreach (var group in groups) {
            var list = group.ToList();

            // Sort by selection order (if available) then sibling index to be deterministic
            list.Sort((a, b) => {
                int oa = orderMap.ContainsKey(a) ? orderMap[a] : int.MaxValue;
                int ob = orderMap.ContainsKey(b) ? orderMap[b] : int.MaxValue;

                if (oa != ob) {
					return oa.CompareTo(ob);
				}

                if (a.parent == b.parent && a.parent != null) {
                    return a.GetSiblingIndex().CompareTo(b.GetSiblingIndex());
				}

                return 0;
            });

            // Record undo for the whole group
            Undo.RecordObjects(list.Select(x => (Object)x).ToArray(), "De-offset selected");

            // Use the first element in the sorted list as the reference
            var first = list[0];

            Vector3 offset = first.localPosition;
            first.localPosition = Vector3.zero;

            for (int i = 1; i < list.Count; i++) {
                list[i].localPosition -= offset;
			}
        }
    }

    // returns true if any descendant (any depth) of 't' is present in selectedSet
    private static bool HasAnyDescendantSelected(Transform t, HashSet<Transform> selectedSet) {
        for (int i = 0; i < t.childCount; i++) {
            var child = t.GetChild(i);

            if (selectedSet.Contains(child)) {
                return true;
			}

            if (HasAnyDescendantSelected(child, selectedSet)) {
                return true;
			}
        }
        return false;
    }
}