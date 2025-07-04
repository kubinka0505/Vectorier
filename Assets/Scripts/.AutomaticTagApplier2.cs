using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Xml;
using System.Globalization;

using Debug = UnityEngine.Debug;

// -=-=-=- //

[InitializeOnLoad]
public class SpriteTagger {
	private static string _FileName = "_SpriteScales.xml";
	private static int _DefaultOrder = 0;
	private static int _DefaultUnits = 0;

	private class SpriteRule {
		public List<string> Names;
		public string Check;
		public string Method;
		public List<string> Components;
		public float Width;
		public float Height;
		public string Tag;
		public int Order;
		public string ColorHex = "#FFFFFFFF";
		public int Units = 0;

		public bool Matches(string spriteName) {
			string nameToCheck = ApplyMethod(spriteName, Method);

			foreach (string matchName in Names) {
				string processed = ApplyMethod(matchName, Method);

				switch (Check.ToLower()) {
					case var s when new[] { "equals", "equal", "is", "equivalent", "equiv", "=", "==", "===" }.Contains(s):
						if (nameToCheck == processed) return true;
						break;
					case var s when new[] { "startswith", "startwith", "starts", "start", "s" }.Contains(s):
						if (nameToCheck.StartsWith(processed)) return true;
						break;
					case var s when new[] { "endswith", "endwith", "ends", "end", "e" }.Contains(s):
						if (nameToCheck.EndsWith(processed)) return true;
						break;
					case var s when new[] { "contains", "contain", "has", "in" }.Contains(s):
					default:
						if (nameToCheck.Contains(processed)) return true;
						break;
				}
			}
			return false;
		}

		private string ApplyMethod(string input, string method) {
			switch (method.ToLower()) {
				case var s when new[] { "tolowercase", "lowercase", "tolower", "lower", "low", "down", "l", "d" }.Contains(s):
					return input.ToLower();
				case var s when new[] { "touppercase", "uppercase", "toupper", "upper", "up", "u" }.Contains(s):
					return input.ToUpper();
				case var s when new[] { "null", "none", "" }.Contains(s):
					// default
				default:
					return input;
			}
		}
	}

	private static readonly List<SpriteRule> rules = new List<SpriteRule>();
	private static string configFilePath;
	private static bool hasInitialized = false;
	private static System.DateTime lastConfigWriteTime = System.DateTime.MinValue;

	// Static constructor
	static SpriteTagger() {
		configFilePath = Path.Combine(Directory.GetParent(GetScriptPath()).ToString(), _FileName);

		EditorApplication.hierarchyChanged += OnHierarchyChanged;
		EditorApplication.update += OnEditorUpdate;
	}

	private static void OnEditorUpdate() {
		if (!hasInitialized) {
			hasInitialized = true;
			LoadSpriteRules();
		} else {
			if (File.Exists(configFilePath)) {
				System.DateTime writeTime = File.GetLastWriteTime(configFilePath);

				if (writeTime > lastConfigWriteTime) {
					LoadSpriteRules();
				}
			}
		}
	}

	private static HashSet<int> processedObjects = new HashSet<int>();

	private static void OnHierarchyChanged() {
		var currentSpriteObjects = new HashSet<int>();

		// Get all GameObjects with SpriteRenderer in the scene
		var allSpriteGOs = GameObject.FindObjectsOfType<SpriteRenderer>(true);

		foreach (var sr in allSpriteGOs) {
			var go = sr.gameObject;

			if (EditorUtility.IsPersistent(go) || PrefabUtility.GetNearestPrefabInstanceRoot(go) != null) {
				continue;
			}

			int id = go.GetInstanceID();
			currentSpriteObjects.Add(id);

			if (!processedObjects.Contains(id) && go.GetComponent<SpriteTaggerFlag>() == null) {
				string spriteName = sr.sprite.name;
				bool matched = false;

				foreach (var rule in rules) {
					if (rule.Matches(spriteName)) {
						AssignRules(go, rule);
						matched = true;
						break;
					}
				}

				if (!matched && IsTagDefined("Image")) {
					var sr_ = go.GetComponent<SpriteRenderer>();

					go.tag = "Image";
					if (sr_ != null) {
						sr_.sortingOrder = _DefaultOrder;
					}
				}

				processedObjects.Add(id);
			}
		}

		// Remove IDs of GameObjects that no longer exist (deleted)
		processedObjects.RemoveWhere(id => !currentSpriteObjects.Contains(id));
	}

	// -=-=-=- //

	private static void LoadSpriteRules() {
		if (!File.Exists(configFilePath)) {
			Debug.LogError("SpriteTagger: XML config not found at: " + configFilePath);
		}

		rules.Clear();

		try {
			var xml = new XmlDocument();
			xml.Load(configFilePath);
			lastConfigWriteTime = File.GetLastWriteTime(configFilePath);

			var spriteNodes = xml.SelectNodes("//Textures/Image");
			foreach (XmlNode node in spriteNodes) {
				var rule = new SpriteRule();

				rule.Names = node.Attributes["ClassName"]?.Value.Split('|').ToList() ?? new List<string>();
				rule.Tag = node.Attributes["Tag"]?.Value ?? "Image";
				rule.Check = node.Attributes["Check"]?.Value ?? "Contains";
				rule.Method = node.Attributes["Method"]?.Value ?? "";
				rule.Components = node.Attributes["Components"]?.Value?.Split('|').ToList() ?? new List<string>();
				rule.Order = ParseInt(node.Attributes["Order"]?.Value, _DefaultOrder);
				rule.ColorHex = node.Attributes["Color"]?.Value ?? "#FFFFFFFF";
				rule.Units = ParseInt(node.Attributes["Units"]?.Value, _DefaultUnits);

				// Dimension rules
				float width = ParseFloat(node.Attributes["Width"]?.Value, -1f);
				float height = ParseFloat(node.Attributes["Height"]?.Value, -1f);

				bool widthProvided = width > 0f;
				bool heightProvided = height > 0f;

				if (widthProvided) {
					rule.Width = width;
					rule.Height = heightProvided ? height : width;
				} else if (heightProvided) {
					rule.Height = height;
					rule.Width = height;
				} else {
					rule.Width = 1f;
					rule.Height = 1f;

					// Assume Units = 1 (Unity units) if no dimensions provided
					rule.Units = 1;
				}

				rules.Add(rule);
			}
		} catch (System.Exception ex) {
			Debug.LogError("SpriteTagger: Failed to load or parse XML config: " + ex.Message);
		}
	}

	private static void AssignRules(GameObject go, SpriteRule rule) {
		var sr = go.GetComponent<SpriteRenderer>();
		if (sr == null) return;

		// Set tag
		if (IsTagDefined(rule.Tag)) {
			go.tag = rule.Tag;
		} else {
			Debug.LogWarning($"SpriteTagger: Tag '{rule.Tag}' is not defined. Skipping tag for {go.name}.");
		}

		// Add marker component to mark as processed
		if (go.GetComponent<SpriteTaggerFlag>() == null) {
			go.AddComponent<SpriteTaggerFlag>();
		}

		// Add components
		foreach (var compName in rule.Components) {
			var type = GetTypeByName(compName); 
			if (type != null && go.GetComponent(type) == null) {
				go.AddComponent(type);
			}
		}

		// Set scale
		float width = rule.Width;
		float height = rule.Height;

		if (rule.Units < 1 && sr.sprite != null) {
			var spriteSize = sr.sprite.rect.size;
			width = rule.Width / spriteSize.x;
			height = rule.Height / spriteSize.y;
		}

		go.transform.localScale = new Vector3(width, height, 1f);

		// Set sorting order
		sr.sortingOrder = rule.Order;

		// Set color
		sr.color = ParseColor(rule.ColorHex);
	}

	private static int ParseInt(string input, int defaultValue = 0) {
		if (int.TryParse(input, out int result)) {
			return result;
		}

		return defaultValue;
	}

	private static float ParseFloat(string input, float defaultValue = 1f) {
		if (string.IsNullOrWhiteSpace(input)) {
			return defaultValue;
		}

		if (float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out float result)) {
			return result;
		}

		return defaultValue;
	}

	private static Color ParseColor(string input) {
		if (string.IsNullOrWhiteSpace(input))
			return Color.white;

		input = input.Trim().ToLowerInvariant();

		// Named colors
		switch (input) {
			case "red": return Color.red;
			case "green": return Color.green;
			case "blue": return Color.blue;
			case "white": return Color.white;
			case "black": return Color.black;
			case "gray":
			case "grey": return Color.gray;
			case "yellow": return Color.yellow;
			case "cyan": return Color.cyan;
			case "magenta": return Color.magenta;
			case "clear": return Color.clear;
		}

		// Hex color parsing
		string hex = input.TrimStart('#').ToUpperInvariant();

		if (hex.Length == 3) // #RGB shorthand
			hex = string.Concat(hex.Select(c => $"{c}{c}")) + "FF";
		else if (hex.Length == 4) // #RGBA shorthand
			hex = string.Concat(hex.Select(c => $"{c}{c}"));
		else if (hex.Length == 6) // #RRGGBB
			hex += "FF";

		if (hex.Length != 8) {
			Debug.LogWarning($"SpriteTagger: Invalid color format '{input}', defaulting to white.");
			return Color.white;
		}

		if (ColorUtility.TryParseHtmlString($"#{hex}", out Color color))
			return color;

		Debug.LogWarning($"SpriteTagger: Failed to parse color '{input}', defaulting to white.");
		return Color.white;
	}

	private static System.Type GetTypeByName(string typeName) {
		var type = System.Type.GetType(typeName);
		if (type != null) {
			return type;
		}

		return System.AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(a => a.GetTypes())
			.FirstOrDefault(t => t.Name == typeName);
	}

	private static bool IsTagDefined(string tag) {
		return UnityEditorInternal.InternalEditorUtility.tags.Contains(tag);
	}

	private static string GetScriptPath() {
		StackTrace stackTrace = new StackTrace(true);

		for (int i = 0; i < stackTrace.FrameCount; i++) {
			if (stackTrace.GetFrame(i).GetMethod() == MethodBase.GetCurrentMethod()) {
				return stackTrace.GetFrame(i + 1).GetFileName();
			}
		}

		return null;
	}

	internal class SpriteTaggerFlag : MonoBehaviour {
		// marker
	}
}