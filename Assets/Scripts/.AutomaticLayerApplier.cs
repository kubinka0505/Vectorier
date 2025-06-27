using UnityEngine;
using UnityEditor;

// -=-=-=- //

[InitializeOnLoad]
public class SpriteLayer {
    static SpriteLayer() {
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }

    private static void OnHierarchyChanged() {
        // Iterate through all GameObjects in the scene
        foreach (GameObject go in GameObject.FindObjectsOfType<GameObject>()) {
            // Check if the GameObject is a prefab instance
            if (PrefabUtility.IsPartOfPrefabInstance(go)) {
                // Skip prefab instances
                continue;
            }

            // Check if it has a SpriteRenderer
            SpriteRenderer spriteRenderer = go.GetComponent<SpriteRenderer>();

            if (spriteRenderer != null) {
                // Check if the sprite is not null
                if (spriteRenderer.sprite != null) {
                    // Assign sortingOrder instead of OrderInLayer
                    spriteRenderer.sortingOrder = -1;
                }
            }
        }
    }
}