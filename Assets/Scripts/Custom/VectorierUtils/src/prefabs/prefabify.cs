using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using VectorierUtils;

// -=-=-=- //

public class PrefabExporter : MonoBehaviour {
    [MenuItem("Vectorier/⚙ Utils/❐ Prefabs/✨ Consolidate selected GameObjects to .prefab", false, 9)]
    public static void PrefabifyManual() {
        ConvertToPrefab(true);
    }

    [MenuItem("Vectorier/⚙ Utils/❐ Prefabs/✨ Consolidate selected GameObjects to .prefab (fast) #%L", false, 9)]
    public static void PrefabifyAutomatic() {
        ConvertToPrefab(false);
    }

	// -=-=-=- //
	// Helpers

    private static void CopyComponentsAndProperties(GameObject source, GameObject destination) {
        destination.tag = source.tag;
        destination.layer = source.layer;

        foreach (var sourceComponent in source.GetComponents<Component>()) {
            if (sourceComponent == null || sourceComponent is Transform) continue;

            System.Type componentType = sourceComponent.GetType();
            Component targetComponent = destination.GetComponent(componentType);

            if (targetComponent == null) {
                targetComponent = destination.AddComponent(componentType);
            }

            EditorUtility.CopySerialized(sourceComponent, targetComponent);
        }
    }

    private static void CopySceneOverridesAndAddedComponents(GameObject source, GameObject destination) {
        // copy scene-added components that don't exist on the raw prefab asset
        foreach (var srcComp in source.GetComponents<Component>()) {
            if (srcComp == null || srcComp is Transform) continue;

            System.Type type = srcComp.GetType();
            
            // check if destination already has this component matching from the prefab base
            var dstComps = destination.GetComponents(type);
            var srcComps = source.GetComponents(type);
            
            // copy extra components added over the original asset if object in scene has them
            if (dstComps.Length < srcComps.Length) {
                Component newComp = destination.AddComponent(type);
                EditorUtility.CopySerialized(srcComp, newComp);
            } else {
                // sync properties/overrides from scene object to destination if a component exists
                int index = System.Array.IndexOf(srcComps, srcComp);

                if (index >= 0 && index < dstComps.Length) {
                    EditorUtility.CopySerialized(srcComp, dstComps[index]);
                }
            }
        }

        // recursively sync children added/modified in scene
        for (int i = 0; i < source.transform.childCount; i++) {
            Transform srcChild = source.transform.GetChild(i);
            Transform dstChild = destination.transform.Find(srcChild.name);

            if (dstChild != null) {
                CopySceneOverridesAndAddedComponents(srcChild.gameObject, dstChild.gameObject);
            }
        }
    }

    private static string GetPenultimateSelectedName() {
        var sortedGameObjects = Selection.gameObjects.OrderBy(go => go.transform.GetSiblingIndex()).ToArray();

        if (sortedGameObjects.Length >= 2) {
            return sortedGameObjects[sortedGameObjects.Length - 2].name;
        }

        return sortedGameObjects[0].name;
    }

    private static GameObject[] GetObjectsToExport() {
        GameObject[] selected = Selection.gameObjects;

        if (selected.Length == 1 && !PrefabUtility.IsPartOfPrefabInstance(selected[0])) {
            Transform parent = selected[0].transform;
            GameObject[] children = new GameObject[parent.childCount];

            for (int i = 0; i < parent.childCount; i++) {
                children[i] = parent.GetChild(i).gameObject;
            }

            return children;
        }

        return selected;
    }

	// -=-=-=- //

    public static void ConvertToPrefab(bool prompt) {
        GameObject[] selected = Selection.gameObjects;
        GameObject[] objectsToExport = GetObjectsToExport();

        if (objectsToExport.Length < 1) {
            Utils.AdvancedLog("Warning", "No GameObjects to prefabify have been selected.");
            return;
        }

        bool isExportingChildren = selected.Length == 1 && !PrefabUtility.IsPartOfPrefabInstance(selected[0]);
        GameObject goParent = isExportingChildren ? selected[0] : null;

        string separator = Path.DirectorySeparatorChar.ToString();
        string randomNumber = UnityEngine.Random.Range(10000, 99999).ToString();

        string firstSelectedGameObjectName = GetPenultimateSelectedName();

        string targetFilePath = prompt ? $"NewPrefab" : firstSelectedGameObjectName;
        targetFilePath = $"_{targetFilePath}_{randomNumber}.prefab";

        string resourcesPath = Path.Combine("Assets", "Resources");
        string prefabPathRelative;

        if (prompt) {
            prefabPathRelative = EditorUtility.SaveFilePanelInProject(
                "Save file",
                Path.GetFileNameWithoutExtension(targetFilePath),
                Path.GetExtension(targetFilePath),
                "Please enter a file name to save",
                resourcesPath
            );
        } else {
            prefabPathRelative = Path.Combine(resourcesPath, targetFilePath);
        }

        if (string.IsNullOrEmpty(prefabPathRelative)) return;

        string prefabPathFull = Path.Combine(
            Path.GetDirectoryName(Application.dataPath),
            prefabPathRelative
        ).Replace("/", separator);

        GameObject root = new GameObject("TempRoot");

        if (goParent != null) {
            root.transform.position = Vector3.zero;
            root.transform.rotation = goParent.transform.rotation;
            root.transform.localScale = goParent.transform.localScale;

            CopyComponentsAndProperties(goParent, root);
        }

        float minX = float.MaxValue;
        float minY = float.MaxValue;

        if (goParent == null) {
            foreach (var goCurrent in objectsToExport) {
                Vector3 pos = goCurrent.transform.position;

                if (pos.x < minX) {
					minX = pos.x;
				}

                if (pos.y < minY) {
					minY = pos.y;
				}
            }
        }

        foreach (var obj in objectsToExport) {
            GameObject objCopy = null;

            // keep original instantiation logic for prefab assets
            if (PrefabUtility.IsPartOfPrefabInstance(obj)) {
                GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(obj);

                if (prefabAsset != null) {
                    objCopy = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);

                    // sync overrides and added components from the scene instance to the instantiated prefab asset
                    CopySceneOverridesAndAddedComponents(obj, objCopy);
                }
            }

            if (objCopy == null) {
                objCopy = Instantiate(obj);
            }

            objCopy.name = obj.name;
            objCopy.tag = obj.tag;

            objCopy.transform.SetParent(root.transform, false);

            if (goParent != null) {
                objCopy.transform.localPosition = obj.transform.localPosition;
                objCopy.transform.localRotation = obj.transform.localRotation;
                objCopy.transform.localScale = obj.transform.localScale;
            } else {
                objCopy.transform.localPosition = new Vector3(
                    obj.transform.position.x - minX,
                    obj.transform.position.y - minY,
                    obj.transform.position.z
                );

                objCopy.transform.localRotation = obj.transform.localRotation;
                objCopy.transform.localScale = obj.transform.localScale;
            }
        }

        if (goParent == null) {
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
        }

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPathRelative);
        DestroyImmediate(root);

        Debug.Log($"Prefab saved to \"{prefabPathFull}\"", savedPrefab);
    }
}