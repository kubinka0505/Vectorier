using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Linq;

using Debug = UnityEngine.Debug;

// -=-=-=- //

[InitializeOnLoad]
public class SpriteTagger {
	// Configuration for sprite name conditions and corresponding tags
	private static readonly Dictionary<System.Predicate<string>, string> tagConfig = new Dictionary<System.Predicate<string>, string> {
		{ name => name.StartsWith("collision"), "Platform" },
		{ name => name.StartsWith("trapezoid_"), "Trapezoid" },
		{ name => name.StartsWith("trigger"), "Trigger" },
		{ name => name.EndsWith("trick"), "Trigger" },
		{ name => name.Contains("_building"), "Backdrop" },
		/*
		{ name => name.StartsWith("hunter_"), "Spawn" },
		*/

		// Default case
		{ name => true, "Image" }
	};

	// Dictionary for sprite scales loaded from a JSON
	private static readonly Dictionary<System.Predicate<string>, Vector2> scaleConfig = new Dictionary<System.Predicate<string>, Vector2>();

	// File path for loading configuration
	private static string jsonFilePath;

	private static string GetScriptPath() {
		StackTrace stackTrace = new StackTrace(true);

		// Find the frame containing this method
		StackFrame frame = null;
		for (int i = 0; i < stackTrace.FrameCount; i++) {
			if (stackTrace.GetFrame(i).GetMethod() == MethodBase.GetCurrentMethod()) {
				// Get the frame where this method was called
				frame = stackTrace.GetFrame(i + 1);
				break;
			}
		}

		return frame.GetFileName();
	}

	static SpriteTagger() {
		jsonFilePath = Path.Combine(Directory.GetParent(GetScriptPath()).ToString(), "sprite_scales.json");
		LoadSpriteScales();
		EditorApplication.hierarchyChanged += OnHierarchyChanged;
	}

	private static void OnHierarchyChanged() {
		// Iterate through all GameObjects in the scene
		foreach (GameObject go in GameObject.FindObjectsOfType<GameObject>()) {
			// Skip prefab instances
			if (PrefabUtility.IsPartOfPrefabInstance(go)) {
				continue;
			}

			// Check if it has a SpriteRenderer
			SpriteRenderer spriteRenderer = go.GetComponent<SpriteRenderer>();

			if (spriteRenderer != null) {
				// Check if the GameObject's tag is not already set
				if (go.tag == "Untagged") {
					// Assign the tag based on the texture name
					if (spriteRenderer.sprite != null) {
						string spriteName = spriteRenderer.sprite.name.ToLower();

						// Find the appropriate tag based on the configuration
						foreach (var entry in tagConfig) {
							if (entry.Key(spriteName)) {
								go.tag = entry.Value;
								break;
							}
						}
 
						// Apply scaling based on sprite name
						foreach (var entry in scaleConfig) {
							if (entry.Key(spriteName)) {
								go.transform.localScale = new Vector3(entry.Value.x, entry.Value.y, 1f);
								break;
							}
						}
					}
				}
			}
		}
	}

	private static void LoadSpriteScales() {
		if (File.Exists(jsonFilePath)) {
			string json = File.ReadAllText(jsonFilePath);

			SimpleSpriteScaleDictionary spriteScaleData = JsonUtility.FromJson<SimpleSpriteScaleDictionary>(json);
			scaleConfig.Clear();

			if (spriteScaleData != null && spriteScaleData.spriteScales != null) {
				// Iterate over each scale entry from the JSON data
				foreach (var scaleEntry in spriteScaleData.spriteScales) {
					// Use IndexOf to match the substring anywhere in the sprite name
					scaleConfig.Add(
						name => name.IndexOf(scaleEntry.name.ToLower(), System.StringComparison.OrdinalIgnoreCase) >= 0,
						new Vector2(scaleEntry.scale[0], scaleEntry.scale[1])
					);
					Debug.Log($"Loaded scale for sprite '{scaleEntry.name}': {scaleEntry.scale[0]}, {scaleEntry.scale[1]}");
				}
			} else {
				Debug.LogError("Invalid sprite scale data in JSON.");
			}
		} else {
			Debug.LogError("JSON file not found: " + jsonFilePath);
			return;
		}
	}


	[System.Serializable]
	private class SimpleSpriteScaleDictionary {
		public List<ScaleEntry> spriteScales;
	}

	[System.Serializable]
	private class ScaleEntry {
		public string name;
		public float[] scale;
	}
}
