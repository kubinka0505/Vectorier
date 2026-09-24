using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#nullable enable
#pragma warning disable CS8600 // nullables
#pragma warning disable CS8601 // nullables
#pragma warning disable CS8602 // nullables

// -=-=-=- //

namespace Vectorier.Settings {
	public static class Core {
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
		public const string KeyGameDirectoryLegacy = "Vectorier.Settings.Game.PropertiesDirectory.Legacy";
		public const string KeyGameDirectoryModern = "Vectorier.Settings.Game.PropertiesDirectory.Modern";

		public const string KeyGameExecutableLegacy = "Vectorier.Settings.Game.Propertiesexecutable.Legacy";
		public const string KeyGameExecutableModern = "Vectorier.Settings.Game.Propertiesexecutable.Modern";

		internal const string KeyGameShortcut = "Vectorier.Settings.Game.PropertiesShortcut.Path";
		internal const string KeyGameShortcutUse = "Vectorier.Settings.Game.PropertiesShortcut.Use";

		internal const string KeyTracksUnusedWrite = "Vectorier.Settings.WriteUnusedTracks";
		internal const string KeyUpdaterEnable = "Vectorier.Updater.Enable";

		// Game internal files
		public static string GameDirectoryLegacy {
			get {
				var parts = new List<string>();

				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
					parts.AddRange(new[] {
						Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
						"Steam"
					});
				} else {
					parts.AddRange(new[] {
						Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
						".steam",
						"steam"
					});
				}

				parts.AddRange(new[] { "steamapps", "common", "Vector" });

				var initialDirectory = Path.Combine(parts.ToArray());

				return EditorPrefs.GetString(KeyGameDirectoryLegacy, initialDirectory);
			} set {
				EditorPrefs.SetString(KeyGameDirectoryLegacy, value);
			}
		}

		public static string GameDirectoryModern {
			get => EditorPrefs.GetString(
				KeyGameDirectoryModern,
				Path.Combine(Directory.GetCurrentDirectory(), "..", "SnailRunner")
			);
			set => EditorPrefs.SetString(KeyGameDirectoryModern, value);
		}
		public static string GameExecutableLegacy {
			get => EditorPrefs.GetString(KeyGameExecutableLegacy, string.Empty);
			set => EditorPrefs.SetString(KeyGameExecutableLegacy, value);
		}
		public static string GameExecutableModern {
			get => EditorPrefs.GetString(KeyGameExecutableModern, string.Empty);
			set => EditorPrefs.SetString(KeyGameExecutableModern, value);
		}

		public static string GameShortcutPath {
			get => EditorPrefs.GetString(KeyGameShortcut, "");
			set => EditorPrefs.SetString(KeyGameShortcut, value);
		}

		public static bool UseShortcutLaunch {
			get => EditorPrefs.GetBool(KeyGameShortcutUse, false);
			set => EditorPrefs.SetBool(KeyGameShortcutUse, value);
		}

		public static bool WriteUnusedTracks {
			get => EditorPrefs.GetBool(KeyTracksUnusedWrite, false);
			set => EditorPrefs.SetBool(KeyTracksUnusedWrite, value);
		}

		public static bool EnableUpdater {
			get => EditorPrefs.GetBool(KeyUpdaterEnable, false);
			set => EditorPrefs.SetBool(KeyUpdaterEnable, value);
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

		// Add this static list
		public static List<string> OrdredAttributes { get; set; } = new List<string>(Vectorier.Core.XML.Utils.General.Attributes);

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

			return Vectorier.Core.Game.Properties.UnitPrecision;
		}

		public static void SetPrecision(
			string key,
			int? value = null
		) {
			int newValue = value ?? EditorPrefs.GetInt(Elements.GlobalPrecisionKey, 3);
			EditorPrefs.SetInt(key, newValue);
		}
	}

	// --- Game ---
	public class SettingsGame : SettingsProvider {
		private string _inputLegacy;
		private string _inputModern;

		private string? _lastLegacyExe = "";
		private string? _lastModernExe = "";

		private bool _lastLegacyResult;
		private bool _lastModernResult;

		// CACHE (prevents constant rescans during OnGUI repaint)
		private static readonly Dictionary<string, string?> _exeCache = new Dictionary<string, string?>();

		public SettingsGame(string path, SettingsScope scope)
			: base(path, scope) {
			_inputLegacy = Core.GameDirectoryLegacy;
			_inputModern = Core.GameDirectoryModern;
		}

		// NORMALIZATION
		private static string NormalizeDisplayPath(string path) {
			if (string.IsNullOrWhiteSpace(path)) {
				return "";
			}

			path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).Trim();

			if (path.Length >= 2 && path[1] == ':') {
				path = char.ToUpper(path[0]) + path.Substring(1);
			}

			return path;
		}

		// EXE RESOLVE (CACHED)
		private static string? ResolveExecutableFromPath(string input, bool expectModern) {
			if (string.IsNullOrWhiteSpace(input)) {
				return null;
			}

			input = NormalizeDisplayPath(input);

			if (_exeCache.TryGetValue(input + expectModern, out var cached)) {
				return cached;
			}

			string? result = null;

			if (File.Exists(input)) {
				result = IsValidExe(input, expectModern) ? input : null;
			} else if (Directory.Exists(input)) {
				foreach (var file in Directory.EnumerateFiles(input, "*.exe")) {
					try {
						Debug.Log(file);
						if (IsValidExe(file, expectModern)) {
							Debug.Log($"executable: {file}");
							result = file;
							break;
						}
					} catch {
						// ...
					}
				}
			}

			_exeCache[input + expectModern] = result;
			return result;
		}

		// VALIDATION
		private static bool IsValidExe(string exePath, bool expectModern) {
			try {
				var v = new Vectorier.Utils.Assets.Executable.Verify(exePath);
				return expectModern ? v.IsModern() : v.IsLegacy();
			} catch {
				return false;
			}
		}

		// SAFE VALIDATION
		private static bool SafeValidate(
			string input,
			bool expectModern,
			out string status,
			out string debug,
			ref string cachedExe,
			ref bool cachedResult
		) {
			status = ""; // todo: fix this
			debug = "";

			if (string.IsNullOrWhiteSpace(input)) {
				return false;
			}

			string? exe = ResolveExecutableFromPath(input, expectModern);

			if (exe == null) {
				status = Directory.Exists(input)
					? "No executables found"
					: "Directory does not exist";

				return false;
			}

			// cache hit
			if (cachedExe == exe) {
				return cachedResult;
			}

			bool modern = false;
			bool legacy = false;

			try {
				var v = new Vectorier.Utils.Assets.Executable.Verify(exe);

				// var dump = v.DumpAll();
				// foreach (var kv in dump) { Debug.Log($"{kv.Key}: {kv.Value}"); }

				modern = v.IsModern();
				legacy = v.IsLegacy();
			} catch (Exception ex) {
				status = "Invalid executable";
				debug = ex.Message;

				return false;
			}

			bool ok = expectModern ? modern : legacy;

			status = ok ? "Valid executable found" : "Invalid executable found";
			debug = $"{exe} | legacy={legacy} modern={modern}";

			// Debug.Log(debug);

			cachedExe = exe;
			cachedResult = ok;

			return ok;
		}

		// ---------------- GUI ----------------
		public override void OnGUI(string searchContext) {
			GUILayout.Space(10);
			Vectorier.Settings.GUILayoutHelpers.LabelField("Paths", fontSize: 13, bold: true);

			// LEGACY
			_inputLegacy = Vectorier.Settings.GUILayoutHelpers.TextField(
				"Game Directory (Legacy)",
				null,
				_inputLegacy
			);

			_inputLegacy = NormalizeDisplayPath(_inputLegacy);

			bool validLegacy = SafeValidate(
				_inputLegacy,
				false,
				out string statusLegacy,
				out _,
				ref _lastLegacyExe,
				ref _lastLegacyResult
			);

			if (!string.IsNullOrWhiteSpace(statusLegacy)) {
				GUILayout.Space(10);
				EditorGUILayout.HelpBox(statusLegacy, validLegacy ? MessageType.Info : MessageType.Warning);
				GUILayout.Space(10);
			}

			// MODERN
			_inputModern = Vectorier.Settings.GUILayoutHelpers.TextField(
				"Game Directory (Modern)",
				null,
				_inputModern
			);

			_inputModern = NormalizeDisplayPath(_inputModern);

			bool validModern = SafeValidate(
				_inputModern,
				true,
				out string statusModern,
				out _,
				ref _lastModernExe,
				ref _lastModernResult
			);

			if (!string.IsNullOrWhiteSpace(statusModern)) {
				GUILayout.Space(10);
				EditorGUILayout.HelpBox(statusModern, validModern ? MessageType.Info : MessageType.Warning);
			}

			// APPLY
			GUILayout.Space(10);

			if (!validLegacy)
				GUI.enabled = false;

			if (GUILayout.Button("Apply Game Directories", GUILayout.Height(40))) {
				if (!string.IsNullOrWhiteSpace(_lastLegacyExe)) {
					Core.GameDirectoryLegacy = Path.GetFullPath(_inputLegacy);
					Core.GameExecutableLegacy = Path.GetFullPath(_lastLegacyExe);
				}

				if (!string.IsNullOrWhiteSpace(_lastModernExe)) {
					Core.GameDirectoryModern = Path.GetFullPath(_inputModern);
					Core.GameExecutableModern = Path.GetFullPath(_lastModernExe);
				}

				Debug.Log(
					"Applied entries:\n" +
					string.Join("\n", new[] {
						Core.GameDirectoryModern,
						Core.GameDirectoryLegacy,
						Core.GameExecutableLegacy,
						Core.GameExecutableModern
					})
				);
			}

			GUI.enabled = true;

			// SHORTCUT
			GUILayout.Space(10);

			Core.GameShortcutPath = Vectorier.Settings.GUILayoutHelpers.TextField(
				"Game Shortcut Path",
				null,
				Core.GameShortcutPath
			);

			Core.UseShortcutLaunch = Vectorier.Settings.GUILayoutHelpers.CheckBox(
				"Use Shortcut Launch",
				null,
				Core.UseShortcutLaunch
			);

			GUILayout.Space(10);
			Vectorier.Settings.GUILayoutHelpers.DrawHorizontalLine();
			GUILayout.Space(10);

			// VARIOUS
			Vectorier.Settings.GUILayoutHelpers.LabelField("Various", fontSize: 14, bold: true);

			Core.WriteUnusedTracks = Vectorier.Settings.GUILayoutHelpers.CheckBox(
				"Write unused tracks",
				null,
				Core.WriteUnusedTracks
			);

			Core.EnableUpdater = Vectorier.Settings.GUILayoutHelpers.CheckBox(
				"Notify about updates",
				null,
				Core.EnableUpdater
			);
		}

		[SettingsProvider]
		public static SettingsProvider CreateProvider()
			=> new SettingsGame("Vectorier/General", SettingsScope.Project);
	}

	public class SettingsElements : SettingsProvider {
		public SettingsElements(string path, SettingsScope scope) : base(path, scope) {
			// ...
		}

		public override void OnGUI(string searchContext) {
			GUILayout.Space(10);

			Core.CenterUnnamedObjects = Vectorier.Settings.GUILayoutHelpers.CheckBox(
				"Center unnamed objects by child positions",
				null,
				Core.CenterUnnamedObjects
			);

			GUILayout.Space(10);

			EditorGUILayout.HelpBox(
				Vectorier.Settings.GUILayoutHelpers.ParseHelpBoxString(
					"For each unnamed <Object> node, set its position to match its most top-left children’s coordinates, and then adjust the children to be relative to that origin."
				),
				MessageType.Info
			);

			GUILayout.Space(10);

			string xmlPath = Path.Combine("Assets", "Scripts", "Vectorier", "Settings", "ExampleCenter.xml");

			XmlDocument doc = new XmlDocument();
			doc.Load(xmlPath);

			XmlDocument workingDoc = (XmlDocument)doc.Clone();
			XmlNode node = workingDoc.DocumentElement;

			if (Core.CenterUnnamedObjects) {
				Vectorier.Core.XML.Utils.Optimize.Objects(
					node,
					Vectorier.Settings.Core.GetPrecision("VectorierSettings.Elements.Properties.Object.Precision")
				);
			}

			GUILayout.Space(10);
			Vectorier.Settings.GUILayoutHelpers.DrawHorizontalLine();
			GUILayout.Space(10);

			if (node != null) {
				Vectorier.Settings.GUILayoutHelpers.LabelField("Preview", fontSize: 14, bold: true);
				GUILayout.Space(5);

				GUI.enabled = false;

				Vectorier.Settings.GUILayoutHelpers.TextAreaDisplay(
					Vectorier.Core.XML.Utils.Optimize.Beautify(node)
				);

				GUI.enabled = true;
			}

			GUILayout.Space(10);
			Vectorier.Settings.GUILayoutHelpers.LabelField($"Length: {node.InnerXml.Length} bytes");
		}

		[SettingsProvider]
		public static SettingsProvider CreateProvider() =>
			new SettingsElements("Vectorier/XML/Nodes/Attributes/Location", SettingsScope.Project);
	}

	// --- Elements / Properties / Precision ---
	public class SettingsElementsPrecision : SettingsProvider {
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
			new SliderData("Area",      Core.Elements.AreaPrecisionKey, 6),
			new SliderData("Backdrop",  Core.Elements.BackdropPrecisionKey, 6),
			new SliderData("Image",     Core.Elements.ImagePrecisionKey, 6),
			new SliderData("Item",      Core.Elements.ItemPrecisionKey, 3),
			new SliderData("Model",     Core.Elements.ModelPrecisionKey, 3),
			new SliderData("Object",    Core.Elements.ObjectPrecisionKey, 3),
			new SliderData("Platform",  Core.Elements.PlatformPrecisionKey, 3),
			new SliderData("Spawn",     Core.Elements.SpawnPrecisionKey, 4),
			new SliderData("Trapezoid", Core.Elements.TrapezoidPrecisionKey, 3),
			new SliderData("Trigger",   Core.Elements.TriggerPrecisionKey, 6),
			new SliderData("TopImage",  Core.Elements.TopImagePrecisionKey, 6),
			new SliderData("Global",    Core.Elements.GlobalPrecisionKey, 3)
		};

		private int[] precisions;

		public SettingsElementsPrecision(string path, SettingsScope scope) : base(path, scope) {
			precisions = new int[sliders.Length];

			for (int i = 0; i < sliders.Length; i++) {
				precisions[i] = Core.GetPrecision(sliders[i].Key, sliders[i].DefaultValue);
			}
		}

		public override void OnGUI(string searchContext) {
			GUILayout.Space(10);

			// --- Sliders ---
			for (int i = 0; i < sliders.Length; i++) {
				if (sliders[i].Label.ToLower().StartsWith("global")) {
					Vectorier.Settings.GUILayoutHelpers.DrawHorizontalLine();
				}

				precisions[i] = EditorGUILayout.IntSlider(sliders[i].Label, precisions[i], 0, 6);
				Core.SetPrecision(sliders[i].Key, precisions[i]);
			}

			GUILayout.Space(10);

			// --- Buttons ---
			using (new EditorGUILayout.HorizontalScope()) {
				if (GUILayout.Button("Set global to all", GUILayout.Height(100))) {
					int global = precisions[sliders.Length - 1];

					for (int i = 0; i < precisions.Length - 1; i++) {
						precisions[i] = global;
						Core.SetPrecision(sliders[i].Key, global);
					}
				}

				if (GUILayout.Button("Reset", GUILayout.Height(100))) {
					for (int i = 0; i < sliders.Length; i++) {
						precisions[i] = sliders[i].DefaultValue;
						Core.SetPrecision(sliders[i].Key, sliders[i].DefaultValue);
					}
				}
			}
		}

		[SettingsProvider]
		public static SettingsProvider CreateProvider() =>
			new SettingsElementsPrecision("Vectorier/XML/Nodes/Attributes/Location/Precision", SettingsScope.Project);
	}

	// XML
	public class SettingsXML : SettingsProvider {
		public SettingsXML(string path, SettingsScope scope) : base(path, scope) { }

		public override void OnGUI(string searchContext) {
			GUILayout.Space(10);

			// 1. Level Slider (linked to Vectorier.Core.XML.Utils.Optimize.DefaultMode)
			Vectorier.Core.XML.Utils.Optimize.DefaultMode = EditorGUILayout.IntSlider(
				"Level",
				Vectorier.Core.XML.Utils.Optimize.DefaultMode,
				1,
				3
			);

			GUILayout.Space(10);

			// 2. Optimization Comparison Table GUI
			DrawOptimizationTable();
		}

		private void DrawOptimizationTable() {
			EditorGUILayout.LabelField("Optimization Modes", EditorStyles.boldLabel);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				// Table Header
				using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar)) {
					GUILayout.Label("Value", EditorStyles.boldLabel, GUILayout.Width(50));
					GUILayout.Label("Method", EditorStyles.boldLabel, GUILayout.Width(100));
					GUILayout.Label("Final file length increment", EditorStyles.boldLabel, GUILayout.Width(180));
					GUILayout.Label("Notes", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
				}

				// Table Rows
				DrawRow("1", "Linearized", -36.73f, "Unreadable one-liner");
				DrawRow("2", "Standard", -16.73f, "");
				DrawRow("3", "Pretty Print", 15.76f, "Intended attributes");
			}
		}

		private void DrawRow(string id, string method, float increment, string notes) {
			string unary = increment > 0 ? "+" : "-";
			string incrementDisplay = Mathf.Abs(increment).ToString("F2", CultureInfo.InvariantCulture);
			incrementDisplay = $"{unary}≳ {incrementDisplay}%";

			using (new EditorGUILayout.HorizontalScope()) {
				GUILayout.Label(id, GUILayout.Width(50));
				GUILayout.Label(method, GUILayout.Width(100));
				GUILayout.Label(incrementDisplay, GUILayout.Width(180));
				GUILayout.Label(notes, GUILayout.ExpandWidth(true));
			}
		}

		[SettingsProvider]
		public static SettingsProvider CreateProvider() =>
			new SettingsXML("Vectorier/XML", SettingsScope.Project);
	}

	// --- XML -> Nodes -> Attributes ---
	public class SettingsXMLAttributes : SettingsProvider {
		private ReorderableList list;

		public SettingsXMLAttributes(string path, SettingsScope scope) : base(path, scope) {
			list = new ReorderableList(Core.OrdredAttributes, typeof(string), true, true, true, true);

			list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, $"Names (x{Core.OrdredAttributes.Count})");

			list.drawElementCallback = (rect, index, isActive, isFocused) => {
				Core.OrdredAttributes[index] = EditorGUI.TextField(rect, Core.OrdredAttributes[index]);
			};

			list.onChangedCallback = _ => {
				Vectorier.Core.XML.Utils.General.Attributes = Core.OrdredAttributes.ToArray();
			};
		}

		public override void OnGUI(string searchContext) {
			GUILayout.Space(10);

			Core.SortNodeAttributes = Vectorier.Settings.GUILayoutHelpers.CheckBox(
				"Sort",
				"Sort nodes.",

				Core.SortNodeAttributes
			);

			GUILayout.Space(10);

			EditorGUILayout.HelpBox(
				Vectorier.Settings.GUILayoutHelpers.ParseHelpBoxString(
					"For each attribute in any node, rewrite it from left to right according to the following, descending order.\n\nRewrites undefined attributes.\nWhitespace and brackets are ignored."
				),
				MessageType.Info
			);

			GUILayout.Space(10);

			list.DoLayoutList();
		}

		[SettingsProvider]
		public static SettingsProvider CreateProvider() =>
			new SettingsXMLAttributes("Vectorier/XML/Nodes/Attributes", SettingsScope.Project);
	}

	// --- Credits ---
	public class SettingsCredits : SettingsProvider {
		public SettingsCredits(string path, SettingsScope scope) : base(path, scope) {
			string creditsPath = Path.Combine(Application.dataPath, "Scripts", "Vectorier", "Settings", "Credits.xml");

			if (File.Exists(creditsPath)) {
				Vectorier.Settings.Credits.Parser.Load(
					creditsPath
				);
			}
		}

		// -=-=-=- //
		// GUI
		public override void OnGUI(string searchContext) {
			string progName = Vectorier.Settings.Credits.Parser.Name;
			var logo = Vectorier.Settings.Credits.Parser.Logo;

			var creators = Vectorier.Settings.Credits.Parser.Creators;
			var maintainersExecutive = Vectorier.Settings.Credits.Parser.MaintainersExecutive;
			var maintainersActive = Vectorier.Settings.Credits.Parser.MaintainersActive;
			var textures = Vectorier.Settings.Credits.Parser.Textures;
			var packages = Vectorier.Settings.Credits.Parser.Packages;
			var acknowledgements = Vectorier.Settings.Credits.Parser.Acknowledgements;

			var UrlCode = Vectorier.Settings.Credits.Parser.UrlCode;
			var UrlChat = Vectorier.Settings.Credits.Parser.UrlChat;

			int space = Vectorier.Settings.Credits.Parser.SpaceVertical;

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
							if (logo == null) {
								GUILayout.Label(progName.ToUpper(), style_h_img, GUILayout.ExpandWidth(true));
							} else {
								GUILayout.Label(logo, GUILayout.Width(500), GUILayout.Height(60), GUILayout.ExpandWidth(false));
							}

							GUILayout.Label("Settings & Credits", style_about, GUILayout.ExpandWidth(true));
							GUILayout.Space(space);

							// --- Creators ---
							GUILayout.Label("Creators", style_h2);
							foreach (var kvp in creators) {
								foreach (var name in kvp.Value) {
									GUILayout.Label(name, style_p);
								}
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
									!string.IsNullOrEmpty(UrlCode) &&
									GUILayout.Button("Source", GUILayout.Width(100), GUILayout.Height(25))
								) {
									Application.OpenURL(UrlCode);
								}

								GUILayout.Space(space);

								if (
									!string.IsNullOrEmpty(UrlChat) &&
									GUILayout.Button("Chat", GUILayout.Width(100), GUILayout.Height(25))
								) {
									Application.OpenURL(UrlChat);
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
			return new SettingsCredits("Vectorier", SettingsScope.Project);
		}
	}
}