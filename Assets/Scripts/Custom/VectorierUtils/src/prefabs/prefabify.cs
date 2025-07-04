using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using VectorierUtils;

public class PrefabExporter : MonoBehaviour {
	[MenuItem("Vectorier/⚙ Utils/❐ Prefabs/✨ Consolidate selected GameObjects to .prefab", false, 9)]
	public static void PrefabifyManual() {
		ConvertToPrefab(true);
	}

	[MenuItem("Vectorier/⚙ Utils/❐ Prefabs/✨ Consolidate selected GameObjects to .prefab (fast) #%L", false, 9)]
	public static void PrefabifyAutomatic() {
		ConvertToPrefab(false);
	}

	private static string GetPenultimateSelectedName() {
		// Sort the selected GameObjects by hierarchy order
		var sortedGameObjects = Selection.gameObjects.OrderBy(go => go.transform.GetSiblingIndex()).ToArray();

		// Find the penultimate GameObject name if possible
		if (sortedGameObjects.Length >= 2) {
			return sortedGameObjects[sortedGameObjects.Length - 2].name;
		}

		// Fallback to the last one if only one GameObject is selected
		return sortedGameObjects[0].name;
	}

	public static void ConvertToPrefab(bool prompt) {
		if (Selection.gameObjects.Length == 0) {
			Utils.AdvancedLog("Warning", "No GameObjects to prefabify have been selected.");
			return;
		}

		// Setup
		string separator = Path.DirectorySeparatorChar.ToString();
		string randomNumber = UnityEngine.Random.Range(10000, 99999).ToString();

		// Determine the penultimate selected GameObject name
		string firstSelectedGameObjectName = GetPenultimateSelectedName();

		string targetFilePath = prompt ? $"NewPrefab" : firstSelectedGameObjectName;
		targetFilePath = $"_{targetFilePath}_{randomNumber}.prefab";

		string resourcesPath = Path.Combine("Assets", "Resources");
		string prefabPathRelative;

		// Ask user where to save the prefab
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

		string prefabPathFull = Path.Combine(
			Path.GetDirectoryName(Application.dataPath),
			prefabPathRelative
		);
		prefabPathFull = prefabPathFull.Replace("/", separator);

		if (!string.IsNullOrEmpty(prefabPathRelative)) {
			float minX = float.MaxValue;
			float minY = float.MaxValue;

			// Find the minimum X and Y position across all selected GameObjects
			foreach (var obj in Selection.gameObjects) {
				Vector3 pos = obj.transform.position;

				if (pos.x < minX) {
					minX = pos.x;
				}

				if (pos.y < minY) {
					minY = pos.y;
				}
			}

			// Create a temporary root object to hold the selected GameObjects
			GameObject root = new GameObject("TempRoot");

			// Instantiate only the selected GameObjects and set their positions
			foreach (var obj in Selection.gameObjects) {
				GameObject objCopy = Instantiate(obj);
				objCopy.name = obj.name;

				// Set each selected object's parent to the temporary root
				objCopy.transform.SetParent(root.transform);

				// Adjust the position of each object relative to the minimum X and Y
				objCopy.transform.position = new Vector3(
					obj.transform.position.x - minX,
					obj.transform.position.y - minY,
					obj.transform.position.z
				);

				objCopy.transform.localRotation = obj.transform.rotation;
				objCopy.transform.localScale = obj.transform.lossyScale;
			}

			// Move the entire root to (0, 0, 0)
			root.transform.position = Vector3.zero;
			root.transform.rotation = Quaternion.identity;

			// Create a prefab from the root, but without including the root GameObject
			// The instantiated objects will be saved directly as they are (without the root container)
			GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPathRelative);

			// Clean up the temporary root object
			DestroyImmediate(root);

			Debug.Log($"Prefab saved to \"{prefabPathFull}\"", savedPrefab);
		}
	}
}