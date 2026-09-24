using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[InitializeOnLoad]
public class SpriteTagger
{
    private static readonly List<(System.Predicate<string> match, string tag)> tagConfig =
        new List<(System.Predicate<string>, string)>
    {
        (name => name.StartsWith("collision"), "Platform"),
        (name => name.StartsWith("trapezoid_"), "Trapezoid"),
        (name => name.StartsWith("trigger"), "Trigger"),
        (name => name.EndsWith("trick"), "Trigger"),
        (name => name.Contains("_building"), "Backdrop"),

        (name => true, "Image")
    };

    static SpriteTagger()
    {
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }

    private static void OnHierarchyChanged()
    {
        foreach (GameObject go in GameObject.FindObjectsOfType<GameObject>())
        {
            if (PrefabUtility.IsPartOfPrefabInstance(go))
                continue;

            // ✅ ONLY modify untagged objects
            if (go.tag != "Untagged")
                continue;

            SpriteRenderer spriteRenderer = go.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null || spriteRenderer.sprite == null)
                continue;

            string spriteName = spriteRenderer.sprite.name.ToLower();

            foreach (var entry in tagConfig)
            {
                if (entry.match(spriteName))
                {
                    Undo.RecordObject(go, "Auto Tag Sprite");

                    go.tag = entry.tag;
                    EditorUtility.SetDirty(go);
                    break;
                }
            }
        }
    }
}