using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;

using Vectorier;

#nullable enable

// -=-=-=- //

namespace Vectorier {
	public static class SettingsHelpers {
		public static float globalLabelWidth = 300;

		public static void LabelField(
			string label,

			int? fontSize = null,

			bool? bold = null,
			bool? italic = null,

			string? tooltip = null,
			float? space = null
		) {
			float labelWidth = space ?? globalLabelWidth;

			GUIStyle style = new GUIStyle(EditorStyles.label);

			if (fontSize.HasValue) {
				style.fontSize = fontSize.Value;
			}

			if (bold.HasValue) {
				style.fontStyle = bold.Value
					? (italic.HasValue && italic.Value ? FontStyle.BoldAndItalic : FontStyle.Bold)
					: (italic.HasValue && italic.Value ? FontStyle.Italic : FontStyle.Normal);
			}

			EditorGUILayout.LabelField(
				new GUIContent(label, tooltip),
				style,
				GUILayout.Width(labelWidth)
			);
		}

		public static string TextArea(
			string label,
			string? tooltip,

			string value,

			float? space = null
		) {
			float labelWidth = space ?? globalLabelWidth;

			EditorGUILayout.BeginHorizontal();

			LabelField(
				label: label,
				tooltip: tooltip,
				space: labelWidth
			);
			value = EditorGUILayout.TextField(value, GUILayout.ExpandWidth(true));

			EditorGUILayout.EndHorizontal();

			return value;
		}

		public static bool CheckBox(
			string label,
			string? tooltip,

			bool value,

			float? space = null
			// bool? left = null;
		) {
			float labelWidth = space ?? globalLabelWidth;
			// bool alignLeft = left ?? true;

			EditorGUILayout.BeginHorizontal();

			LabelField(
				label: label,
				tooltip: tooltip,
				space: labelWidth
			);
			value = EditorGUILayout.Toggle(value, GUILayout.ExpandWidth(true));

			EditorGUILayout.EndHorizontal();

			return value;
		}

		public static void DrawHorizontalLine(
			float height = 1.25f,

			float margin_top = 8f,
			float margin_bottom = 7f,

			Color? color = null
		) {
			Color lineColor;

			if (color.HasValue) {
				lineColor = color.Value;
			} else {
				float col = EditorGUIUtility.isProSkin ? col = 0.4f : 0.6f;
				lineColor = new Color(col, col, col, 1f);
			}

			GUILayout.Space(margin_top);

			Rect rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(height));
			EditorGUI.DrawRect(rect, lineColor);

			GUILayout.Space(margin_bottom);
		}

		public static string ParseHelpBoxString(
			string value,
			char sep = ' ',
			int sepMul = 3,
			int wrap = 50,
			bool wrapEndWord = true
		) {
			if (string.IsNullOrEmpty(value)) return "";

			value = value.Trim(); // preserve blank lines at the end

			string indent = new string(sep, sepMul);

			// normalize lines: keep empty lines intact
			var lines = value
				.Split('\n')
				.Select(line => line.TrimEnd())
				.Select(line => string.IsNullOrWhiteSpace(line) ? "" : indent + line.TrimStart())
				.ToArray();

			var wrappedLines = lines.SelectMany(line =>
			{
				if (string.IsNullOrWhiteSpace(line)) return new[] { "" }; // preserve empty lines
				return WrapLine(line, wrap, wrapEndWord, indent);
			});

			return "\n" + string.Join("\n", wrappedLines) + "\n";
		}

		private static IEnumerable<string> WrapLine(
			string line,
			int wrap,
			bool wrapEndWord,
			string indent
		) {
			int start = 0;
			bool firstLine = true;

			while (start < line.Length) {
				int remaining = line.Length - start;
				int count = Math.Min(wrap, remaining);

				if (wrapEndWord && count < remaining) {
					int lastSpace = line.LastIndexOf(' ', start + count);
					if (lastSpace > start) count = lastSpace - start;
				}

				string segment = line.Substring(start, count).TrimEnd();

				// first line already has indentation; subsequent wrapped lines keep same indent
				if (!firstLine)
					yield return indent + segment;
				else
					yield return segment;

				firstLine = false;
				start += count;

				// skip leading spaces
				while (start < line.Length && line[start] == ' ') start++;
			}
		}
	}

	public static class Settings {
		// --- Elements / Properties / Precision ---
		public static class Elements {
			public const string AreaPrecisionKey      = "Vectorier.Settings.Elements.Properties.Area.Precision";
			public const string AnimationPrecisionKey = "Vectorier.Settings.Elements.Properties.Animation.Precision";
			public const string BackdropPrecisionKey  = "Vectorier.Settings.Elements.Properties.Backdrop.Precision";
			public const string CameraPrecisionKey    = "Vectorier.Settings.Elements.Properties.Camera.Precision";
			public const string ImagePrecisionKey     = "Vectorier.Settings.Elements.Properties.Image.Precision";
			public const string ItemPrecisionKey      = "Vectorier.Settings.Elements.Properties.Item.Precision";
			public const string ModelPrecisionKey     = "Vectorier.Settings.Elements.Properties.Model.Precision";
			public const string ObjectPrecisionKey    = "Vectorier.Settings.Elements.Properties.Object.Precision";
			public const string PlatformPrecisionKey  = "Vectorier.Settings.Elements.Properties.Platform.Precision";
			public const string SpawnPrecisionKey     = "Vectorier.Settings.Elements.Properties.Spawn.Precision";
			public const string TrapezoidPrecisionKey = "Vectorier.Settings.Elements.Properties.Trapezoid.Precision";
			public const string TriggerPrecisionKey   = "Vectorier.Settings.Elements.Properties.Trigger.Precision";
			public const string TopImagePrecisionKey  = "Vectorier.Settings.Elements.Properties.TopImage.Precision";
			public const string GlobalPrecisionKey    = "Vectorier.Settings.Elements.Properties.Any.Precision";
		}

		// --- Game ---
		internal const string GameDirectoryKey = "Vectorier.Settings.GameDirectory";
		internal const string GameShortcutKey = "Vectorier.Settings.GameShortcutPath";
		internal const string UseShortcutLaunchKey = "Vectorier.Settings.UseShortcutLaunch";
		internal const string WriteUnusedTracksKey = "Vectorier.Settings.WriteUnusedTracks";
		internal const string UpdaterKey = "Vectorier.Updater";

		public static string GameDirectory {
			get => EditorPrefs.GetString(GameDirectoryKey, @"C:\Program Files (x86)\Steam\steamapps\common\Vector");
			set => EditorPrefs.SetString(GameDirectoryKey, value);
		}

		public static string GameShortcutPath {
			get => EditorPrefs.GetString(GameShortcutKey, "");
			set => EditorPrefs.SetString(GameShortcutKey, value);
		}

		public static bool UseShortcutLaunch {
			get => EditorPrefs.GetBool(UseShortcutLaunchKey, false);
			set => EditorPrefs.SetBool(UseShortcutLaunchKey, value);
		}

		public static bool WriteUnusedTracks {
			get => EditorPrefs.GetBool(WriteUnusedTracksKey, false);
			set => EditorPrefs.SetBool(WriteUnusedTracksKey, value);
		}

		public static bool Updater {
			get => EditorPrefs.GetBool(UpdaterKey, false);
			set => EditorPrefs.SetBool(UpdaterKey, value);
		}

		// --- Scene ---
		internal const string SaveSceneBeforeBuildMapKey = "Vectorier.Settings.SaveSceneBeforeBuild";
		internal const string ValidateSceneKey = "Vectorier.Settings.ValidateScene";
		internal const string ValidateWrittenTrackXmlKey = "Vectorier.Settings.Validate.Track.Current.XML";

		public static bool SaveSceneBeforeBuildMap {
			get => EditorPrefs.GetBool(SaveSceneBeforeBuildMapKey, true);
			set => EditorPrefs.SetBool(SaveSceneBeforeBuildMapKey, value);
		}

		public static bool ValidateScene {
			get => EditorPrefs.GetBool(ValidateSceneKey, true);
			set => EditorPrefs.SetBool(ValidateSceneKey, value);
		}

		public static bool ValidateWrittenTrackXml {
			get => EditorPrefs.GetBool(ValidateWrittenTrackXmlKey, false);
			set => EditorPrefs.SetBool(ValidateWrittenTrackXmlKey, value);
		}

		// --- Elements / Properties ---
		internal const string CenterUnnamedObjectsKey = "Vectorier.Settings.Elements.Properties.Object.CoordinatesAdjuster.Use";
		public static bool CenterUnnamedObjects {
			get => EditorPrefs.GetBool(CenterUnnamedObjectsKey, false);
			set => EditorPrefs.SetBool(CenterUnnamedObjectsKey, value);
		}

		// --- XML ---
		internal const string SortNodeAttributesKey = "Vectorier.Settings.XML.Attributes.Sort";
		public static bool SortNodeAttributes {
			get => EditorPrefs.GetBool(SortNodeAttributesKey, true);
			set => EditorPrefs.SetBool(SortNodeAttributesKey, value);
		}

		// <-- Add this static list -->
		public static List<string> OrdredAttributes { get; set; } = new List<string>(Vectorier.Core.XML.Utils.Attributes);

		// helper to get array directly
		// public static string[] OrdredAttributesArray => OrdredAttributes.ToArray();

		// Sliders
		internal static readonly string[,] ElementPrecisionKeys = new string[,] {
			{ "Area", "Vectorier.Settings.Elements.Properties.Area.Precision" },
			{ "Animation", "Vectorier.Settings.Elements.Properties.Animation.Precision" },
			{ "Camera", "Vectorier.Settings.Elements.Properties.Camera.Precision" },
			{ "Backdrop", "Vectorier.Settings.Elements.Properties.Backdrop.Precision" },
			{ "Image", "Vectorier.Settings.Elements.Properties.Image.Precision" },
			{ "Item", "Vectorier.Settings.Elements.Properties.Item.Precision" },
			{ "Model", "Vectorier.Settings.Elements.Properties.Model.Precision" },
			{ "Object", "Vectorier.Settings.Elements.Properties.Object.Precision" },
			{ "Platform", "Vectorier.Settings.Elements.Properties.Platform.Precision" },
			{ "Spawn", "Vectorier.Settings.Elements.Properties.Spawn.Precision" },
			{ "Trapezoid", "Vectorier.Settings.Elements.Properties.Trapezoid.Precision" },
			{ "Trigger", "Vectorier.Settings.Elements.Properties.Trigger.Precision" },
			{ "TopImage", "Vectorier.Settings.Elements.Properties.TopImage.Precision" },
			{ "Global", "Vectorier.Settings.Elements.Properties.Any.Precision" }
		};

		public static int GetPrecision(
			string key,
			int? defaultValue = null
		) {
			if (EditorPrefs.HasKey(key)) {
				return EditorPrefs.GetInt(key);
			}

			if (defaultValue.HasValue) {
				return defaultValue.Value;
			}

			if (EditorPrefs.HasKey(Elements.GlobalPrecisionKey)) {
				return EditorPrefs.GetInt(Elements.GlobalPrecisionKey);
			}

			return Vectorier.Core.Game.UnitPrecision;
		}

		public static void SetPrecision(
			string key,
			int? value = null
		) {
			int newValue = value ?? EditorPrefs.GetInt(Elements.GlobalPrecisionKey, 3);
			EditorPrefs.SetInt(key, newValue);
		}
	}

	// Vectorier Settings Providers

	// --- Game ---
	public class SettingsGameProvider : SettingsProvider {
		public SettingsGameProvider(string path, SettingsScope scope) : base(path, scope) { }

		public override void OnGUI(string searchContext) {
			GUILayout.Space(10);

			SettingsHelpers.LabelField("Paths", fontSize: 13, bold: true);
			
			Settings.GameDirectory = SettingsHelpers.TextArea(
				"Game Directory",
				"Vectorier.Settings.Game.Source.Location",

				Settings.GameDirectory
			);

			Settings.GameShortcutPath = SettingsHelpers.TextArea(
				"Game Shortcut Path",
				"Vectorier.Settings.Game.Shortcut.Location",

				Settings.GameShortcutPath
			);

			Settings.UseShortcutLaunch = SettingsHelpers.CheckBox(
				"Use Shortcut Launch",
				"Vectorier.Settings.Game.Shortcut.Use",

				Settings.UseShortcutLaunch
			);

			GUILayout.Space(10);

			SettingsHelpers.LabelField("Various", fontSize: 14, bold: true);

			Settings.WriteUnusedTracks = SettingsHelpers.CheckBox(
				"Write unused tracks",
				"Vectorier.Settings.Game.WriteUnusedTracks",

				Settings.WriteUnusedTracks
			);

			Settings.Updater = SettingsHelpers.CheckBox(
				"Notify about updates",
				"Vectorier.Updater",

				Settings.Updater
			);
		}

		[SettingsProvider]
		public static SettingsProvider CreateProvider() =>
			new SettingsGameProvider("Vectorier/General", SettingsScope.Project);
	}

	// --- Scene ---
	public class SettingsSceneProvider : SettingsProvider {
		public SettingsSceneProvider(string path, SettingsScope scope) : base(path, scope) {
			// ...
		}

		public override void OnGUI(string searchContext) {
			GUILayout.Space(10);
			
			Settings.SaveSceneBeforeBuildMap = SettingsHelpers.CheckBox(
				"Save before build",
				"Save scene before map is compiled.",

				Settings.SaveSceneBeforeBuildMap
			);

			GUILayout.Space(10);

			SettingsHelpers.LabelField("Validate", fontSize: 13, bold: true);

			Settings.ValidateScene = SettingsHelpers.CheckBox(
				"Scene",
				"Perform several checks for scene integrity.",

				Settings.ValidateScene
			);

			Settings.ValidateWrittenTrackXml = SettingsHelpers.CheckBox(
				"XML (display window)",
				"Display window with XML content before saving the map.",

				Settings.ValidateWrittenTrackXml
			);
		}

		[SettingsProvider]
		public static SettingsProvider CreateProvider() =>
			new SettingsSceneProvider("Vectorier/Scene", SettingsScope.Project);
	}

	public class SettingsElementsProvider : SettingsProvider {
		public SettingsElementsProvider(string path, SettingsScope scope) : base(path, scope) {
			// ...
		}

		public override void OnGUI(string searchContext) {
			GUILayout.Space(10);

			Settings.CenterUnnamedObjects = SettingsHelpers.CheckBox(
				"Center unnamed objects by child positions",
				null,

				Settings.CenterUnnamedObjects
			);

			GUILayout.Space(10);

			EditorGUILayout.HelpBox(SettingsHelpers.ParseHelpBoxString(
				"For each unnamed <Object> node, set its position to match its most top-left children’s coordinates, and then adjust the children to be relative to that origin."),
				MessageType.Info
			);
		}

		[SettingsProvider]
		public static SettingsProvider CreateProvider() =>
			new SettingsElementsProvider("Vectorier/XML/Nodes/Attributes/Location", SettingsScope.Project);
	}

	// --- Elements / Properties / Precision ---
	public class SettingsElementsPrecisionProvider : SettingsProvider {
		private class SliderData {
			public string Label;
			public string Key;
			public int DefaultValue;

			public SliderData(string label, string key, int defaultValue) {
				Label = label;
				Key = key;
				DefaultValue = defaultValue;
			}
		}

		private SliderData[] sliders = new SliderData[] {
			new SliderData("Area",      Settings.Elements.AreaPrecisionKey, 6),
			new SliderData("Backdrop",  Settings.Elements.BackdropPrecisionKey, 6),
			new SliderData("Image",     Settings.Elements.ImagePrecisionKey, 6),
			new SliderData("Item",      Settings.Elements.ItemPrecisionKey, 3),
			new SliderData("Model",     Settings.Elements.ModelPrecisionKey, 3),
			new SliderData("Object",    Settings.Elements.ObjectPrecisionKey, 3),
			new SliderData("Platform",  Settings.Elements.PlatformPrecisionKey, 3),
			new SliderData("Spawn",     Settings.Elements.SpawnPrecisionKey, 4),
			new SliderData("Trapezoid", Settings.Elements.TrapezoidPrecisionKey, 3),
			new SliderData("Trigger",   Settings.Elements.TriggerPrecisionKey, 6),
			new SliderData("TopImage",  Settings.Elements.TopImagePrecisionKey, 6),
			new SliderData("Global",    Settings.Elements.GlobalPrecisionKey, 3)
		};

		private int[] precisions;

		public SettingsElementsPrecisionProvider(string path, SettingsScope scope) : base(path, scope) {
			precisions = new int[sliders.Length];

			for (int i = 0; i < sliders.Length; i++) {
				precisions[i] = Settings.GetPrecision(sliders[i].Key, sliders[i].DefaultValue);
			}
		}

		public override void OnGUI(string searchContext) {
			GUILayout.Space(10);

			// --- Sliders ---
			for (int i = 0; i < sliders.Length; i++) {
				if (sliders[i].Label.ToLower().StartsWith("global")) {
					SettingsHelpers.DrawHorizontalLine();
				}

				precisions[i] = EditorGUILayout.IntSlider(sliders[i].Label, precisions[i], 0, 6);
				Settings.SetPrecision(sliders[i].Key, precisions[i]);
			}

			GUILayout.Space(10);

			// --- Buttons ---
			using (new EditorGUILayout.HorizontalScope()) {
				if (GUILayout.Button("Set global to all", GUILayout.Height(100))) {
					int global = precisions[sliders.Length - 1];

					for (int i = 0; i < precisions.Length - 1; i++) {
						precisions[i] = global;
						Settings.SetPrecision(sliders[i].Key, global);
					}
				}

				if (GUILayout.Button("Reset", GUILayout.Height(100))) {
					for (int i = 0; i < sliders.Length; i++) {
						precisions[i] = sliders[i].DefaultValue;
						Settings.SetPrecision(sliders[i].Key, sliders[i].DefaultValue);
					}
				}
			}
		}

		[SettingsProvider]
		public static SettingsProvider CreateProvider() =>
			new SettingsElementsPrecisionProvider("Vectorier/XML/Nodes/Attributes/Location/Precision", SettingsScope.Project);
	}

	// --- XML ---
	public class SettingsXmlProvider : SettingsProvider {
		private ReorderableList list;

		public SettingsXmlProvider(string path, SettingsScope scope) : base(path, scope) {
			list = new ReorderableList(Settings.OrdredAttributes, typeof(string), true, true, true, true);

			list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, $"Names (x{Settings.OrdredAttributes.Count})");

			list.drawElementCallback = (rect, index, isActive, isFocused) => {
				Settings.OrdredAttributes[index] = EditorGUI.TextField(rect, Settings.OrdredAttributes[index]);
			};

			list.onChangedCallback = _ => {
				Vectorier.Core.XML.Utils.Attributes = Settings.OrdredAttributes.ToArray();
			};
		}

		public override void OnGUI(string searchContext) {
			GUILayout.Space(10);

			Settings.SortNodeAttributes = SettingsHelpers.CheckBox(
				"Sort",
				"Sort nodes.",

				Settings.SortNodeAttributes
			);

			GUILayout.Space(10);

			EditorGUILayout.HelpBox(SettingsHelpers.ParseHelpBoxString(
				"For each attribute in any node, rewrite it from left to right according to the following, descending order.\n\nRewrites undefined attributes.\nWhitespace and brackets are ignored."),
				MessageType.Info
			);

			GUILayout.Space(10);

			list.DoLayoutList();
		}

		[SettingsProvider]
		public static SettingsProvider CreateProvider() =>
			new SettingsXmlProvider("Vectorier/XML/Nodes/Attributes", SettingsScope.Project);
	}

	// --- About ---
	public class UserData {
		public string Name = "";
		public string? Url = null;
		public List<string> Aliases;

		public UserData() {
			Aliases = new List<string>();
		}
	}

	public class MaintainerData {
		public string Name = "";
		public string Period = "";
	}

	public class SettingsAboutProvider : SettingsProvider {
		// static UI fields
		public static readonly int space = 18;
		public static readonly string progName = "Vectorier";
		public static readonly Texture2D progLogo = AssetDatabase.LoadAssetAtPath<Texture2D>(
			Path.Combine("Assets", "Scripts", "Vectorier", "Logo.png") // progname doesn't work
		);

		// dynamic data
		private Dictionary<string, UserData> userMap;
		private string? codeUrl;
		private string? chatUrl;
		private string? projectName;

		private Dictionary<string, List<string>> creators;
		private Dictionary<string, List<MaintainerData>> maintainersActive;
		private Dictionary<string, List<MaintainerData>> maintainersExecutive;
		private Dictionary<string, List<string>> textures;
		private Dictionary<string, List<string>> packages;
		private Dictionary<string, List<string>> acknowledgements;

		public SettingsAboutProvider(string path, SettingsScope scope) : base(path, scope) {
			userMap = new Dictionary<string, UserData>();
			creators = new Dictionary<string, List<string>>();

			maintainersActive = new Dictionary<string, List<MaintainerData>>();
			maintainersExecutive = new Dictionary<string, List<MaintainerData>>();

			textures = new Dictionary<string, List<string>>();
			packages = new Dictionary<string, List<string>>();

			acknowledgements = new Dictionary<string, List<string>>();

			LoadXml(
				Path.Combine(Application.dataPath, "Scripts", "Vectorier", "About.xml")
			);
		}

		private void LoadXml(string xmlPath) {
			try {
				if (!File.Exists(xmlPath))
					return;

				string xmlText = File.ReadAllText(xmlPath);

				// Wrap the XML in a single root element
				xmlText = $"<Root>{xmlText}</Root>";

				XmlDocument doc = new XmlDocument();
				doc.LoadXml(xmlText);

				// --- Users ---
				XmlNode usersNode = doc.SelectSingleNode("//Root/Info/Users");
				if (usersNode != null) {
					foreach (XmlNode node in usersNode.SelectNodes("User")) {
						UserData u = new UserData();
						string[] nameParts = node.Attributes["Name"]?.Value.Split('|') ?? new string[] { "Unknown" };
						u.Name = nameParts[0];
						u.Aliases.AddRange(nameParts);
						u.Url = node.Attributes["URL"]?.Value;
						userMap[u.Name] = u;
					}
				}

				// --- Project ---
				projectName = doc.SelectSingleNode("//Root/Project")?.Attributes["Name"]?.Value ?? "Unknown";

				// --- Social links ---
				XmlNode socialNode = doc.SelectSingleNode("//Root/Info/Social");
				codeUrl = socialNode?.SelectSingleNode("Code")?.Attributes["URL"]?.Value.Replace("{progName}", projectName) ?? "";
				chatUrl = socialNode?.SelectSingleNode("Chat")?.Attributes["URL"]?.Value ?? "";

				// --- Creators ---
				XmlNodeList creatorNodes = doc.SelectNodes("//Root/Project/Creators/User");
				foreach (XmlNode node in creatorNodes) {
					string rawName = node.Attributes["Name"]?.Value ?? "Unknown";
					List<string> resolved = ResolveAliases(rawName);
					string name = resolved.Count > 0 ? resolved[0] : rawName;
					creators[name] = new List<string> { name };
				}

				// --- Maintainers: Active ---
				XmlNodeList activeNodes = doc.SelectNodes("//Root/Project/Maintainers/Active/User");
				foreach (XmlNode node in activeNodes) {
					string rawName = node.Attributes["Name"]?.Value ?? "Unknown";
					List<string> resolved = ResolveAliases(rawName);
					string name = resolved.Count > 0 ? resolved[0] : rawName;
					string period = FormatPeriod(node.Attributes["Start"]?.Value, node.Attributes["End"]?.Value);
					maintainersActive[name] = new List<MaintainerData> { new MaintainerData { Name = name, Period = period } };
				}

				// --- Maintainers: Executive ---
				XmlNodeList execNodes = doc.SelectNodes("//Root/Project/Maintainers/Executive/User");
				foreach (XmlNode node in execNodes) {
					string rawName = node.Attributes["Name"]?.Value ?? "Unknown";
					List<string> resolved = ResolveAliases(rawName);
					string name = resolved.Count > 0 ? resolved[0] : rawName;
					string period = FormatPeriod(node.Attributes["Start"]?.Value, node.Attributes["End"]?.Value);
					maintainersExecutive[name] = new List<MaintainerData> { new MaintainerData { Name = name, Period = period } };
				}

				// --- Textures ---
				XmlNodeList textureNodes = doc.SelectNodes("//Root/Project/Assets/Textures/Asset");
				foreach (XmlNode node in textureNodes) {
					string name = node.Attributes["Name"]?.Value ?? "Unknown";
					string authors = node.Attributes["Authors"]?.Value ?? "";
					textures[name] = ResolveAliases(authors);
				}

				// --- Packages ---
				XmlNodeList packageNodes = doc.SelectNodes("//Root/Project/Assets/Packages/Asset");
				foreach (XmlNode node in packageNodes) {
					string name = node.Attributes["Name"]?.Value ?? "Unknown";
					string authors = node.Attributes["Authors"]?.Value ?? "";
					packages[name] = ResolveAliases(authors);
				}

				// --- Acknowledgements ---
				XmlNodeList ackNodes = doc.SelectNodes("//Root/Project/Acknowledgements/User");
				foreach (XmlNode node in ackNodes) {
					string rawName = node.Attributes["Name"]?.Value ?? "Unknown";
					string contribs = node.Attributes["Contributions"]?.Value ?? "";

					List<string> resolved = ResolveAliases(rawName);
					string name = resolved.Count > 0 ? resolved[0] : rawName;

					acknowledgements[name] = new List<string>(contribs.Split('|'));
				}
			} catch (Exception ex) {
				Debug.LogError($"Failed to load About XML: {ex}");
			}
		}

		private List<string> ResolveAliases(string authors) {
			List<string> result = new List<string>();

			foreach (string a in authors.Split('|')) {
				foreach (var kvp in userMap) {
					if (kvp.Value.Aliases.Contains(a)) {
						result.Add(kvp.Value.Name);
						break;
					}
				}
			}

			return result;
		}

		private string FormatPeriod(string? startUnix, string? endUnix) {
			string startStr = string.Empty;
			string endStr = string.Empty;

			if (!string.IsNullOrEmpty(startUnix)) {
				if (long.TryParse(startUnix, out long startVal)) {
					startStr = FormatDate(startVal);
				}
			}

			if (!string.IsNullOrEmpty(endUnix)) {
				if (long.TryParse(endUnix, out long endVal)) {
					endStr = FormatDate(endVal);
				}
			}

			if (string.IsNullOrEmpty(startStr) && string.IsNullOrEmpty(endStr))
				return "date unknown";

			if (!string.IsNullOrEmpty(startStr) && !string.IsNullOrEmpty(endStr))
				return $"from {startStr} to {endStr}";

			if (!string.IsNullOrEmpty(startStr))
				return $"from {startStr}";

			return $"to {endStr}";
		}

		private string FormatDate(
			long timestamp,
			bool useSystemLocale = true
		) {
			DateTime dt = DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;

			// Day with leading zero + ordinal
			string dayWithSuffix = dt.ToString("dd") + GetDaySuffix(dt.Day);

			// Month and year
			string monthYear = useSystemLocale
				? dt.ToString("MMMM yyyy", CultureInfo.CurrentCulture)
				: dt.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

			return $"{dayWithSuffix} {monthYear}";
		}

		private string GetDaySuffix(int day) {
			switch (day) {
				case 1:
				case 21:
				case 31:
					return "st";
				case 2:
				case 22:
					return "nd";
				case 3:
				case 23:
					return "rd";
				default:
					return "th";
			}
		}

		// -=-=-=- //
		// GUI
		public override void OnGUI(string searchContext) {
			// --- GUI Styles ---
			GUIStyle style_h_img = new GUIStyle(EditorStyles.boldLabel) {
				alignment = TextAnchor.MiddleCenter,
				fontSize = 48,
				fontStyle = FontStyle.Bold
			};

			GUIStyle style_h = new GUIStyle(EditorStyles.boldLabel) {
				alignment = TextAnchor.MiddleCenter,
				fontSize = 38,
				fontStyle = FontStyle.Bold,
				normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
			};

			GUIStyle style_about = new GUIStyle(EditorStyles.label) {
				alignment = TextAnchor.MiddleCenter,
				fontSize = 13,
				richText = true,
				fontStyle = FontStyle.BoldAndItalic,
				normal = { textColor = Color.gray }
			};

			GUIStyle style_h2 = new GUIStyle(EditorStyles.boldLabel) {
				alignment = TextAnchor.MiddleCenter,
				fontSize = 20,
				fontStyle = FontStyle.Bold
			};

			GUIStyle style_h1 = new GUIStyle(EditorStyles.boldLabel) {
				alignment = TextAnchor.MiddleCenter,
				fontSize = 24,
				fontStyle = FontStyle.Bold
			};

			GUIStyle style_p = new GUIStyle(EditorStyles.label) {
				alignment = TextAnchor.MiddleCenter,
				fontSize = 12,
				wordWrap = true
			};

			GUIStyle style_h3_b = new GUIStyle(EditorStyles.label) {
				alignment = TextAnchor.MiddleCenter,
				fontSize = 14,
				wordWrap = true,
				fontStyle = FontStyle.BoldAndItalic
			};

			GUIStyle style_sub1 = new GUIStyle(EditorStyles.label) {
				alignment = TextAnchor.MiddleCenter,
				fontSize = 10,
				wordWrap = true,
				richText = true,
				fontStyle = FontStyle.BoldAndItalic,
				normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
			};

			GUIStyle style_sub = new GUIStyle(EditorStyles.label) {
				alignment = TextAnchor.MiddleCenter,
				fontSize = 11,
				wordWrap = true,
				richText = true
			};

			try {
				using (new EditorGUILayout.VerticalScope()) {
					using (new EditorGUILayout.HorizontalScope()) {
						GUILayout.FlexibleSpace();

						using (new EditorGUILayout.VerticalScope(GUILayout.Width(500))) {
							// Logo
							if (progLogo == null) {
								GUILayout.Label(progName.ToUpper(), style_h_img, GUILayout.ExpandWidth(true));
							} else {
								GUILayout.Label(progLogo, GUILayout.Width(500), GUILayout.Height(60), GUILayout.ExpandWidth(false));
							}

							GUILayout.Label("Settings & Credits", style_about, GUILayout.ExpandWidth(true));
							GUILayout.Space(space);

							// --- Creators ---
							GUILayout.Label("Creators", style_h2);
							foreach (var kvp in creators) {
								foreach (var name in kvp.Value) GUILayout.Label(name, style_p);
							}

							GUILayout.Space(space);
							GUILayout.Label("Maintainers", style_h1);
							GUILayout.Space(space);

							// --- Maintainers Active ---
							GUILayout.Label("Active", style_h2);

							foreach (var kvp in maintainersActive) {
								foreach (var md in kvp.Value) {
									GUILayout.Label($"{md.Name} ({md.Period})", style_p);
								}
							}

							GUILayout.Space(space / 2);
							// --- Maintainers Executive ---

							GUILayout.Label("Executive", style_h2);
							foreach (var kvp in maintainersExecutive) {
								foreach (var md in kvp.Value) {
									GUILayout.Label($"{md.Name} ({md.Period})", style_p);
								}
							}

							GUILayout.Space(space);
							GUILayout.Label("Assets", style_h1);
							GUILayout.Space(space);

							// --- Textures ---
							GUILayout.Label("Textures", style_h2);
							GUILayout.Space(space / 4);

							foreach (var kvp in textures) {
								GUILayout.Label(kvp.Key, style_h3_b);
	  
								foreach (var author in kvp.Value) {
									GUILayout.Label(author, style_sub);
								}

								GUILayout.Space(space / 2);
							}

							GUILayout.Space(space / 2);

							// --- Packages ---
							GUILayout.Label("Packages", style_h2);
							GUILayout.Space(space / 4);

							foreach (var kvp in packages) {
								GUILayout.Label(kvp.Key, style_h3_b);

								foreach (var author in kvp.Value) {
									GUILayout.Label(author, style_sub);
								}

								GUILayout.Space(space / 2);
							}

							GUILayout.Space(space / 2);

							// --- Acknowledgements ---
							GUILayout.Label("Special thanks", style_h1);
							GUILayout.Space(space);

							foreach (var kvp in acknowledgements) {
								GUILayout.Label(kvp.Key, style_h3_b);

								foreach (var contribution in kvp.Value) {
									GUILayout.Label(contribution, style_sub);
								}

								GUILayout.Space(space / 2);
							}

							GUILayout.Space(space);

							// --- Buttons ---
							using (new EditorGUILayout.HorizontalScope()) {
								GUILayout.FlexibleSpace();

								if (
									!string.IsNullOrEmpty(codeUrl) &&
									GUILayout.Button("Source", GUILayout.Width(100), GUILayout.Height(25))
								) {
									Application.OpenURL(codeUrl);
								}

								GUILayout.Space(space);

								if (
									!string.IsNullOrEmpty(chatUrl) &&
									GUILayout.Button("Chat", GUILayout.Width(100), GUILayout.Height(25))
								) {
									Application.OpenURL(chatUrl);
								}

								GUILayout.FlexibleSpace();
							}

							GUILayout.Space(space);
						}
						GUILayout.FlexibleSpace();
					}
				}
			} catch (Exception ex) {
				Debug.LogError($"OnGUI error: {ex}");
			}
		}

		[SettingsProvider]
		public static SettingsProvider CreateProvider() {
			return new SettingsAboutProvider("Vectorier", SettingsScope.Project);
		}
	}
}