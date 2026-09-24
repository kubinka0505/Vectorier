using UnityEngine;
using UnityEditor;

using System.IO;
using System.Xml;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;

using Vectorier;

// -=-=-=- //

public class RetextureWindow : EditorWindow {
	[MenuItem("Vectorier/Utils/Retexture")]
	public static void ShowWindow() {
		var window = GetWindow<RetextureWindow>("Retexture");

		window.minSize = new Vector2(350, 85);
		window.maxSize = new Vector2(60, 115);
	}

	// --- //
	// Variables
	private string scriptPath;
	private string scriptName; 

	public bool debugMode;

	private List<ThemeEntry> themes = new List<ThemeEntry>();
	private int selectedThemeIndex = 0;

	public string directoryThemes;

	// --- //
	// Enablers

	private void OnEnable() {
		scriptPath = Vectorier.Utils.Internal.Script.GetPath();
		scriptName = Path.GetFileNameWithoutExtension(scriptPath);

		debugMode = false;

		directoryThemes = Path.Combine(
			Path.GetDirectoryName(scriptPath),
			"Themes"
		);

		LoadThemes();
	}

	private void OnGUI() {
		if (themes.Count == 0) {
			EditorGUILayout.HelpBox("No valid themes found.", MessageType.Warning);

			if (GUILayout.Button("Reload")) {
				LoadThemes();
			}

			return;
		}

		string[] names = themes
			.Select(t => $"{t.GroupPath}/{t.Theme.Name} ({t.Theme.Textures.Count} changes)")
			.ToArray();

	    EditorGUILayout.BeginHorizontal();

		selectedThemeIndex = EditorGUILayout.Popup(
			string.Empty,

			selectedThemeIndex,
			names

			// GUILayout.Height(30),
			// GUILayout.ExpandHeight(true)
		);

		if (GUILayout.Button(
			"↺",

			GUILayout.Width(25)
			// GUILayout.ExpandHeight(true))
		)) {
			LoadThemes();
		}

		EditorGUILayout.EndHorizontal();

		GUILayout.Space(5);

		if (GUILayout.Button(
			"Apply",

			GUILayout.Height(60)
		)) {
			ApplyTheme(themes[selectedThemeIndex].Theme);
		}

		debugMode = GUILayout.Toggle(debugMode, "Debug mode", GUILayout.Height(30));
	}

	// --- //
	// Core

	public string GetRelativeFolder(string file) {
		string rel = file.Replace(directoryThemes, string.Empty).TrimStart(
			Path.DirectorySeparatorChar,
			Path.AltDirectorySeparatorChar
		);
		string dir = Path.GetDirectoryName(rel);
		return string.IsNullOrEmpty(dir) ? "Root" : dir;
	}

	bool ValidateXmlThemeFile(string file, XmlDocument doc) {
		if (!File.Exists(file)) {
			if (debugMode) {
				Debug.LogWarning($"[{scriptName}] File missing: {file}");
			}

			return false;
		}

		if (new FileInfo(file).Length == 0) {
			if (debugMode) {
				Debug.LogWarning($"[{scriptName}] Empty XML file: {file}");
			}

			return false;
		}

		try {
			doc.Load(file);
		} catch (System.Exception e) {
			if (debugMode) {
				Debug.LogWarning($"[{scriptName}] XML parse error: {file}\n{e.Message}");
			}

			return false;
		}

		// Root check
		if (doc.DocumentElement == null || doc.DocumentElement.Name != "Map") {
			if (debugMode) {
				Debug.LogWarning($"[{scriptName}] Invalid root node (expected <Map>): {file}");
			}

			return false;
		}

		var themes = doc.SelectNodes("//Theme");
		if (themes == null || themes.Count == 0) {
			if (debugMode) {
				Debug.LogWarning($"[{scriptName}] No <Theme> nodes found: {file}");
			}

			return false;
		}

		foreach (XmlNode theme in themes) {
			var textures = theme.SelectNodes("Texture");

			if (textures == null || textures.Count == 0) {
				if (debugMode) {
					Debug.LogWarning($"[{scriptName}] Theme missing <Texture>: {file}");
				}

				return false;
			}

			foreach (XmlNode tex in textures) {
				var input = tex.Attributes?["Input"]?.Value;

				if (string.IsNullOrEmpty(input)) {
					if (debugMode) {
						Debug.LogWarning($"[{scriptName}] Texture missing Input attribute: {file}");
					}

					return false;
				}
			}
		}

		return true;
	}

	void LoadThemes() {
		themes.Clear();

		if (!Directory.Exists(directoryThemes)) {
			if (debugMode) {
				Debug.LogError($"[{scriptName}] Folder not found: {directoryThemes}");
			}

			return;
		}

		var files = Directory.GetFiles(directoryThemes, "*.xml", SearchOption.AllDirectories);

		if (debugMode) {
			Debug.Log($"[{scriptName}] Loading XMLs: {files.Length}");
		}

		List<ThemeEntry> loaded = new List<ThemeEntry>();

		foreach (var file in files) {
			XmlDocument doc = new XmlDocument();

			if (!ValidateXmlThemeFile(file, doc)) {
				continue;
			}

			string relativePath = GetRelativeFolder(file);
			string folderName = Path.GetFileName(Path.GetDirectoryName(file));

			foreach (XmlNode themeNode in doc.SelectNodes("//Theme")) {
				ThemeData theme = new ThemeData();
				theme.Name = GetAttr(themeNode, "Name");
				theme.Suffix = GetAttr(themeNode, "Suffix");

				foreach (XmlNode texNode in themeNode.SelectNodes("Texture")) {
					TextureData tex = new TextureData();

					tex.Input = GetAttr(texNode, "Input");
					tex.Target = GetAttr(texNode, "Target");

					tex.AddPos = new Vector3(
						GetFloat(texNode, "AddPosX"),
						GetFloat(texNode, "AddPosY"),
						GetFloat(texNode, "AddPosZ")
					);

					tex.AddRot = new Vector3(
						GetFloat(texNode, "AddRotX"),
						GetFloat(texNode, "AddRotY"),
						GetFloat(texNode, "AddRotZ")
					);

					tex.AddScale = new Vector3(
						GetFloat(texNode, "AddScaleX"),
						GetFloat(texNode, "AddScaleY"),
						GetFloat(texNode, "AddScaleZ")
					);

					theme.Textures.Add(tex);
				}

				if (theme.Textures.Count == 0) {
					continue;
				}

				loaded.Add(new ThemeEntry {
					Theme = theme,
					GroupPath = relativePath
				});
			}
		}

		// merge duplicates by (folder + theme name)
		themes = loaded
			.GroupBy(t => t.GroupPath + "|" + t.Theme.Name)
			.Select(g => {
				var first = g.First();

				return new ThemeEntry {
					GroupPath = first.GroupPath,
					Theme = new ThemeData {
						Name = first.Theme.Name,
						Suffix = first.Theme.Suffix,
						Textures = g.SelectMany(x => x.Theme.Textures)
							.GroupBy(t => $"{t.Input}|{t.Target}")
							.Select(x => x.First())
							.ToList()
					}
				};
			}
		).ToList();

		if (debugMode) {
			Debug.Log($"[{scriptName}] Loaded themes: {themes.Count}");
		}
	}

	void ApplyTheme(ThemeData theme) {
		if (debugMode) {
			Debug.Log($"[{scriptName}] Applying theme: {theme.Name}");
		}

		List<GameObject> targets = new List<GameObject>();

		if (Selection.gameObjects.Length > 0) {
			foreach (var go in Selection.gameObjects) {
				targets.AddRange(GetAllChildren(go));
			}

			if (debugMode) {
				Debug.Log($"[{scriptName}] Using selection: {targets.Count} all objects");
			}
		} else {
			targets = FindObjectsOfType<SpriteRenderer>()
				.Select(sr => sr.gameObject)
				.ToList();

			if (debugMode) {
				Debug.Log($"[{scriptName}] Using whole scene: {targets.Count} all objects");
			}
		}

		int matches = 0;

		Undo.IncrementCurrentGroup();
		int group = Undo.GetCurrentGroup();

		foreach (var go in targets) {
			var sr = go.GetComponent<SpriteRenderer>();
			if (sr == null || sr.sprite == null) {
				continue;
			}

			foreach (var tex in theme.Textures) {
				string spriteName = (sr.sprite.name ?? "").Trim();
				string inputName = (tex.Input ?? "").Trim();

				if (debugMode) {
					Debug.Log($"[{scriptName}] Compare '{spriteName}' vs '{inputName}'");
				}

				if (!string.Equals(spriteName, inputName, System.StringComparison.OrdinalIgnoreCase)) {
					continue;
				}

				matches++;
				if (debugMode) {
					Debug.Log($"[{scriptName}] MATCH: {go.name} ({spriteName})");
				}

				Undo.RecordObject(go.transform, "Retexture Transform");
				Undo.RecordObject(go, "Retexture Name");

				go.transform.position += tex.AddPos;
				go.transform.eulerAngles += tex.AddRot;
				go.transform.localScale += tex.AddScale;

				string newName = spriteName;

				if (!string.IsNullOrEmpty(tex.Target)) {
					string suffix = theme.Suffix ?? "";
					newName = tex.Target.Replace(suffix, "");
				} else if (!string.IsNullOrEmpty(theme.Suffix)) {
					newName = spriteName + theme.Suffix;
				}

				string baseName = ExtractBaseName(go.name);
				string extra = go.name.Substring(baseName.Length);
				go.name = newName + extra;

				Sprite newSprite = FindSprite(newName);
				if (newSprite != null) {
					Undo.RecordObject(sr, "Retexture Sprite");
					sr.sprite = newSprite;
				} else if (debugMode) {
					Debug.LogWarning($"[{scriptName}] Sprite not found: {newName}");
				}
			}
		}

		Undo.CollapseUndoOperations(group);

		if (debugMode) {
			Debug.Log($"[{scriptName}] Done. Matches: {matches}");
		}
	}

	// Helpers
	List<GameObject> GetAllChildren(GameObject root) {
		return root.GetComponentsInChildren<Transform>(true)
			.Select(t => t.gameObject)
			.ToList();
	}

	string GetAttr(XmlNode node, string name) {
		return node.Attributes?[name]?.Value ?? "";
	}

	float GetFloat(XmlNode node, string name) {
		float val;

		if (float.TryParse(GetAttr(node, name), NumberStyles.Float, CultureInfo.InvariantCulture, out val))
			return val;

		return 0f;
	}

	string ExtractBaseName(string name) {
		int idx = name.IndexOfAny(new char[] { '(', '[', '{' });

		if (idx >= 0) {
			return name.Substring(0, idx).TrimEnd();
		}

		return name;
	}

	Sprite FindSprite(string name) {
		string[] guids = AssetDatabase.FindAssets(name + " t:Sprite");

		foreach (var guid in guids) {
			string path = AssetDatabase.GUIDToAssetPath(guid);
			var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>();

			foreach (var s in sprites) {
				if (s.name == name) {
					return s;
				}
			}
		}

		return null;
	}

	// Data
	class ThemeData {
		public string Name;
		public string Suffix;
		public List<TextureData> Textures = new List<TextureData>();
	}

	class ThemeEntry {
		public ThemeData Theme;
		public string DisplayName;
		public string GroupPath;
	}

	class TextureData {
		public string Input;
		public string Target;
		public Vector3 AddPos;
		public Vector3 AddRot;
		public Vector3 AddScale;
	}
}