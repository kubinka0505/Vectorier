using UnityEditor;
using UnityEngine;

public class MoveChildrenToRootUtilitya {
    [MenuItem("Tools/Move All Children and Remove Empty Parents #%J")]
    private static void MoveChildrenToRoot() {
        GameObject root = Selection.activeGameObject;
        if (root == null) {
            Debug.LogWarning("No GameObject selected. Please select a GameObject.");
            return;
        }

        // Register the root object for undo
        Undo.RegisterCompleteObjectUndo(root, "Move Children to Root");

        // Handle prefab cases
        bool isPrefab = PrefabUtility.IsPartOfPrefabInstance(root);

        // Get all children including inactive ones
        Transform[] children = root.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children) {
            if (child == root.transform) {
                continue;
            }

            // Register undo for each child's parent change
            Undo.SetTransformParent(child, root.transform, "Move Children to Root");
            child.parent = root.transform;
        }

        // Iterate through children again to check for empty parents
        foreach (Transform child in children) {
            if (child == root.transform) {
                continue;
            }

            // Check if the GameObject only has a Transform component
            if (child.GetComponents<Component>().Length == 1) {
                // Register undo for destroying the GameObject
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        // Mark the root as dirty for unsaved changes
        EditorUtility.SetDirty(root);

        // Mark Prefab instance as dirty if applicable
        if (isPrefab) {
            PrefabUtility.RecordPrefabInstancePropertyModifications(root);
        }

        Debug.Log("Moved all children and removed empty parent objects. Prefabs marked as dirty where applicable.");
    }
}
