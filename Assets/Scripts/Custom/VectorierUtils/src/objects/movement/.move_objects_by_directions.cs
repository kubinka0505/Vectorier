using UnityEngine;
using UnityEditor;
using VectorierUtils;

public class MoveGameObjectsUtility2 : EditorWindow
{
    // Store whether the shortcut is being held down
    static bool isCtrlArrowDown = false;

    // Add menu item to open the window
    [MenuItem("Vectorier/⚙ Utils/✥ Objects positioning/Move on CTRL+Arrow Down")]
    public static void ShowWindow()
    {
        // Open the EditorWindow
        GetWindow<MoveGameObjectsUtility2>("Move Objects Utility");
    }

    // This will continuously listen for the keypress while the window is open
    void OnGUI()
    {
        // Check for key press every time the window is drawn
        if (Event.current != null && Event.current.type == EventType.KeyDown)
        {
            if (Event.current.control && Event.current.keyCode == KeyCode.DownArrow)
            {
                if (!isCtrlArrowDown)
                {
                    isCtrlArrowDown = true;
                    Move_5_Bottom(); // Trigger the action when both CTRL and Down Arrow are pressed
                }
            }
            else
            {
                isCtrlArrowDown = false;
            }
        }
    }

    // The original method that moves objects when called
    [MenuItem("Vectorier/⚙ Utils/✥ Objects positioning/↓ 2.50% Bottom #%&M", false, 8)]
    static void Move_5_Bottom()
    {
        Move_Selected_Objects(0.025f, -0.025f);
    }

    // Main method to move selected objects
    static void Move_Selected_Objects(float xpos, float ypos)
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            if (obj != null)
            {
                if (PrefabUtility.IsAnyPrefabInstanceRoot(obj))
                {
                    Move_Prefab_Instance(obj, xpos, ypos);
                }
                else
                {
                    Move_Regular_Object(obj, xpos, ypos);
                }
            }
        }
    }

    // Moves regular objects by the specified offset
    static void Move_Regular_Object(GameObject obj, float xpos, float ypos)
    {
        if (obj.TryGetComponent<Renderer>(out Renderer renderer))
        {
            Undo.RecordObject(obj.transform, "Move Object");

            Vector3 size = renderer.bounds.size;
            obj.transform.position += new Vector3(size.x * xpos, size.y * ypos, 0);

            EditorUtility.SetDirty(obj.transform);
        }
        else
        {
            Utils.AdvancedLog("Warning", $"{obj.name} does not have any Renderer components, cannot determine size.");
        }
    }

    // Moves prefab instances by the specified offset
    static void Move_Prefab_Instance(GameObject obj, float xpos, float ypos)
    {
        // Iterate over all the Renderers in the prefab instance
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Undo.RecordObject(obj.transform, "Move Object");

            // Calculate the bounding box of all renderers
            Bounds bounds = new Bounds(obj.transform.position, Vector3.zero);
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            Vector3 size = bounds.size;
            Vector3 newPosition = obj.transform.position + new Vector3(size.x * xpos, size.y * ypos, 0);
            obj.transform.position = newPosition;

            // Mark the prefab instance as dirty to apply changes
            EditorUtility.SetDirty(obj);

            // Apply changes to the prefab asset
            PrefabUtility.RecordPrefabInstancePropertyModifications(obj);

            // Set dirty to update prefab asset changes
            EditorUtility.SetDirty(obj.transform);
        }
        else
        {
            Utils.AdvancedLog("Warning", $"{obj.name} does not have any Renderer components, cannot determine size.");
        }
    }
}
