using UnityEngine;
using UnityEditor;
using System.IO;

// -=-=-=- //

public class CopyAsset {
	[MenuItem("Assets/❐ Copy", false, 20)]
	private static void CopySelectedAsset() {
		string path = AssetDatabase.GetAssetPath(Selection.activeObject);
		if (string.IsNullOrEmpty(path)) {
			Debug.LogWarning("No asset selected to copy");
			return;
		}

		string extension = Path.GetExtension(path).ToLower();

		string directory = Path.GetDirectoryName(path);
		string filename = Path.GetFileNameWithoutExtension(path);

		// start with first copy as " - copy"
		string newFilename = filename + " - copy";
		string newPath = Path.Combine(directory, newFilename + extension);

		int copyNumber = 1;

		// repeat
		while (AssetDatabase.LoadAssetAtPath<Object>(newPath) != null) {
			newFilename = $"{filename} - copy ({copyNumber})";
			newPath = Path.Combine(directory, newFilename + extension);
			copyNumber++;
		}

		AssetDatabase.CopyAsset(path, newPath);
		AssetDatabase.Refresh();

		Object newAsset = AssetDatabase.LoadAssetAtPath<Object>(newPath);
		Selection.activeObject = newAsset;

		Debug.Log($"Copied {path} to {newPath}", newAsset);
	}

	[MenuItem("Assets/❐ Copy", true)]
	private static bool ValidateCopySelectedAsset() {
		return Selection.activeObject != null;
	}
}